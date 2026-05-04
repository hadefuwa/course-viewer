// scripts/extract.mjs
// Parse the source .docx into site/src/data/course.json and copy images
// from source/Media/ into site/src/assets/images/.
//
// The source document uses pseudo-XML markers in its body text to delimit
// content for downstream tooling, e.g.:
//
//   <worksheet> <filename> "CP4807-1.doc" </filename>
//   ...content...
//   </worksheet>
//
// Inline images appear as Word "linked images" (<a:blip r:link="rIdN">) where
// the relationships file points at absolute file:// paths on the original
// author's machine. We resolve the basename and re-link to local Media/ files.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import AdmZip from "adm-zip";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "..");
const SOURCE_DIR = path.join(ROOT, "source");
const MEDIA_DIR = path.join(SOURCE_DIR, "Media");
const SITE_DATA_DIR = path.join(ROOT, "site", "src", "data");
const SITE_IMG_DIR = path.join(ROOT, "site", "public", "images");

// ---------- find the docx ----------
const docxName = fs
  .readdirSync(SOURCE_DIR)
  .find((f) => f.toLowerCase().endsWith(".docx"));
if (!docxName) {
  console.error("No .docx found in source/");
  process.exit(1);
}
const docxPath = path.join(SOURCE_DIR, docxName);
console.log(`> Reading ${docxName}`);

// ---------- unzip in memory ----------
const zip = new AdmZip(docxPath);
const readEntry = (name) => {
  const e = zip.getEntry(name);
  if (!e) throw new Error(`Missing entry in docx: ${name}`);
  return e.getData().toString("utf8");
};
const documentXml = readEntry("word/document.xml");
const relsXml = readEntry("word/_rels/document.xml.rels");

// ---------- build rId -> filename map ----------
const rels = {};
for (const m of relsXml.matchAll(
  /<Relationship\b[^>]*Id="([^"]+)"[^>]*Target="([^"]+)"[^>]*\/?>/g
)) {
  const id = m[1];
  let target = decodeURIComponent(m[2].replace(/\\/g, "/"));
  // strip file:/// prefix and any path; keep basename
  const base = target.split(/[\\/]/).pop();
  rels[id] = base;
}

// ---------- copy/normalise images ----------
fs.mkdirSync(SITE_IMG_DIR, { recursive: true });
const mediaFiles = fs.existsSync(MEDIA_DIR)
  ? fs.readdirSync(MEDIA_DIR)
  : [];
// Index by a normalised key so " " / "_" / "-" all collide.
const mediaKey = (s) => s.toLowerCase().replace(/[\s_\-]+/g, "_");
const mediaIndex = new Map(mediaFiles.map((f) => [mediaKey(f), f]));

const normaliseName = (name) =>
  name
    .toLowerCase()
    .replace(/\s+/g, "-")
    .replace(/[^a-z0-9.\-_]/g, "");

const copiedImages = new Map(); // originalBasename -> normalisedName
const missingImages = new Set();

function resolveImage(originalBasename) {
  if (!originalBasename) return null;
  if (copiedImages.has(originalBasename))
    return copiedImages.get(originalBasename);

  const actual = mediaIndex.get(mediaKey(originalBasename));
  if (!actual) {
    missingImages.add(originalBasename);
    return null;
  }
  // skip oddities like .cpt (CorelDraw) — viewer can't render them
  const ext = path.extname(actual).toLowerCase();
  if (![".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"].includes(ext)) {
    return null;
  }
  const norm = normaliseName(actual);
  fs.copyFileSync(path.join(MEDIA_DIR, actual), path.join(SITE_IMG_DIR, norm));
  copiedImages.set(originalBasename, norm);
  return norm;
}

// ---------- walk paragraphs ----------
// For each <w:p>, we extract:
//   - text:     concatenated <w:t> children (entity-decoded)
//   - drawings: list of rIds referenced by <a:blip r:link|r:embed>
//   - styleId:  pPr/pStyle val (helps detect headings)
//
// Then we group paragraphs into worksheets bounded by literal text markers
// "<worksheet>" and "</worksheet>".

const decodeEntities = (s) =>
  s
    .replace(/&lt;/g, "<")
    .replace(/&gt;/g, ">")
    .replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'")
    .replace(/&amp;/g, "&")
    .replace(/[“”]/g, '"')
    .replace(/[‘’]/g, "'");

function paragraphs(xml) {
  const out = [];
  const re = /<w:p(?:\s[^>]*)?>([\s\S]*?)<\/w:p>/g;
  let m;
  while ((m = re.exec(xml))) {
    const inner = m[1];
    const text = decodeEntities(
      [...inner.matchAll(/<w:t(?:\s[^>]*)?>([\s\S]*?)<\/w:t>/g)]
        .map((mm) => mm[1])
        .join("")
    );
    const styleMatch = inner.match(/<w:pStyle\s+w:val="([^"]+)"/);
    const styleId = styleMatch ? styleMatch[1] : null;
    const drawings = [];
    for (const dm of inner.matchAll(/<w:drawing\b[\s\S]*?<\/w:drawing>/g)) {
      const block = dm[0];
      for (const bm of block.matchAll(
        /<a:blip\b[^>]*r:(?:link|embed)="([^"]+)"/g
      )) {
        drawings.push(bm[1]);
      }
    }
    out.push({ text, styleId, drawings });
  }
  return out;
}

const paras = paragraphs(documentXml);
console.log(`> ${paras.length} paragraphs`);

// ---------- group into worksheets ----------
const worksheets = [];
let current = null;
const startRe = /<\s*worksheet\s*>/i;
const endRe = /<\/\s*worksheet\s*>/i;
const filenameRe = /<\s*filename\s*>\s*"?([^"<]+?)"?\s*<\/\s*filename\s*>/i;
const explicitImgRe = /<\s*image\s*>\s*([^<]+?)\s*<\/\s*image\s*>/i;

for (const p of paras) {
  const t = p.text;

  if (startRe.test(t)) {
    if (current) worksheets.push(current);
    const fn = (t.match(filenameRe) || [, ""])[1].trim();
    current = { filename: fn, blocks: [] };
    continue;
  }
  if (!current) continue;
  if (endRe.test(t)) {
    worksheets.push(current);
    current = null;
    continue;
  }

  // images first (preserves any inline ordering well enough at paragraph level)
  for (const rId of p.drawings) {
    const orig = rels[rId];
    const local = resolveImage(orig);
    if (local) current.blocks.push({ type: "image", src: local, alt: orig });
  }
  // explicit <image>foo.jpg</image> markers (the test one)
  const ex = t.match(explicitImgRe);
  if (ex) {
    const local = resolveImage(ex[1].trim());
    if (local)
      current.blocks.push({ type: "image", src: local, alt: ex[1].trim() });
  }

  // text content (skip if paragraph was purely an image marker / empty)
  let cleaned = t.replace(explicitImgRe, "").trim();
  if (cleaned) {
    current.blocks.push({
      type: "p",
      style: p.styleId || null,
      text: cleaned,
    });
  }
}
if (current) worksheets.push(current);

console.log(`> ${worksheets.length} worksheet sections`);

// ---------- structure each worksheet ----------
// Each worksheet's filename hints at its role:
//   CP4807-head.doc      -> cover
//   CP4807-Cont.doc      -> contents
//   CP4807-0.doc         -> preparation
//   CP4807-1..12.doc     -> worksheets 1..12
//   CP4807-TN.doc/etc    -> teacher's notes
//
// We classify, then for numbered worksheets we segment blocks at known
// section headings: "Over to you:", "Challenges:", "Hints:", "Part N",
// "Hardware:", "Software:".

const TIER_BY_NUMBER = {
  1: "Bronze",
  2: "Bronze",
  3: "Bronze",
  4: "Bronze",
  5: "Bronze",
  6: "Bronze",
  7: "Bronze",
  8: "Silver",
  9: "Silver",
  10: "Silver",
  11: "Gold",
  12: "Gold",
};

const SECTION_LABELS = [
  "Over to you:",
  "Challenges:",
  "Hints:",
  "Hardware:",
  "Software:",
  "Preparation:",
];

const isYouTube = (s) => /https?:\/\/(youtu\.be|(www\.)?youtube\.com)/i.test(s);
const ytId = (url) => {
  const m =
    url.match(/youtu\.be\/([A-Za-z0-9_-]{6,})/) ||
    url.match(/[?&]v=([A-Za-z0-9_-]{6,})/);
  return m ? m[1].split(/[?&#]/)[0] : null;
};

function classify(filename) {
  const f = filename.toLowerCase();
  if (f.includes("head")) return { kind: "cover" };
  if (f.includes("cont")) return { kind: "contents" };
  if (/-0\.doc/.test(f)) return { kind: "preparation" };
  if (/-tn\.doc|teacher/.test(f)) return { kind: "teacher" };
  const m = f.match(/-(\d{1,2})\.doc/);
  if (m) {
    const n = parseInt(m[1], 10);
    return { kind: "worksheet", number: n, tier: TIER_BY_NUMBER[n] || null };
  }
  return { kind: "other" };
}

function structureWorksheet(ws) {
  const meta = classify(ws.filename);
  const result = { ...meta, filename: ws.filename };

  // pull a hero image (first image block) and title (first non-empty text)
  let heroImage = null;
  let title = null;
  let subtitle = null;
  const remaining = [];
  for (const b of ws.blocks) {
    if (!heroImage && b.type === "image") {
      heroImage = b.src;
      continue;
    }
    if (!title && b.type === "p") {
      title = b.text;
      continue;
    }
    if (!subtitle && b.type === "p" && /microcontroller/i.test(b.text)) {
      // "Introduction to microcontrollers" recurring subtitle line
      subtitle = b.text;
      continue;
    }
    remaining.push(b);
  }
  // For numbered worksheets the docx pattern is:
  //   p1 = "Worksheet N"   (we keep as `label`)
  //   p2 = topic title     (promote to `title`)
  let label = title;
  let displayTitle = title;
  if (meta.kind === "worksheet") {
    // find the next plain paragraph that isn't the recurring subtitle
    const overviewBlocks = (() => {
      const arr = [];
      for (const b of remaining) {
        if (b.type !== "p") continue;
        const t = b.text.trim();
        if (/^introduction to(\s+microcontrollers)?$/i.test(t)) continue;
        if (/^microcontrollers$/i.test(t)) continue;
        arr.push(b);
        if (arr.length >= 1) break;
      }
      return arr;
    })();
    if (overviewBlocks[0]) {
      displayTitle = overviewBlocks[0].text.trim();
      // remove that block from remaining so it doesn't dupe in Overview
      const idx = remaining.indexOf(overviewBlocks[0]);
      if (idx >= 0) remaining.splice(idx, 1);
    }
  }
  result.label = label;
  result.title = displayTitle;
  result.subtitle = subtitle;
  result.heroImage = heroImage;

  // strip the recurring "Introduction to microcontrollers" subtitle wherever
  // it appears as its own paragraph
  const cleaned = remaining.filter(
    (b) =>
      !(
        b.type === "p" &&
        /^introduction to\s*$|^microcontrollers$|^introduction to microcontrollers$/i.test(
          b.text.trim()
        )
      )
  );

  // split into sections
  const sections = [];
  let currentSection = { heading: "Overview", blocks: [] };
  for (const b of cleaned) {
    if (b.type === "p") {
      const text = b.text.trim();
      const matchedLabel = SECTION_LABELS.find(
        (l) => text.toLowerCase() === l.toLowerCase()
      );
      const partMatch = text.match(/^Part\s+\d+\b/i);
      if (matchedLabel) {
        if (currentSection.blocks.length || currentSection.heading !== "Overview")
          sections.push(currentSection);
        currentSection = { heading: matchedLabel.replace(/:$/, ""), blocks: [] };
        continue;
      }
      if (partMatch) {
        if (currentSection.blocks.length || currentSection.heading !== "Overview")
          sections.push(currentSection);
        currentSection = { heading: text, blocks: [] };
        continue;
      }
      // detect youtube link as its own block
      if (isYouTube(text)) {
        const id = ytId(text);
        currentSection.blocks.push({ type: "youtube", url: text, id });
        continue;
      }
      currentSection.blocks.push({ type: "p", text });
    } else {
      currentSection.blocks.push(b);
    }
  }
  if (currentSection.blocks.length) sections.push(currentSection);
  result.sections = sections;
  return result;
}

const structured = worksheets.map(structureWorksheet);

// ---------- build top-level course doc ----------
const course = {
  code: "CP4807",
  title: "Introduction to Microcontrollers",
  unit: "BTEC Unit 6 — Microcontrollers",
  hours: 60,
  publisher: "Matrix Technology Solutions",
  source: docxName,
  generatedAt: new Date().toISOString(),
  tiers: [
    { name: "Bronze", range: [1, 7], blurb: "Foundations: first programs, I/O, decisions, macros, prototyping." },
    { name: "Silver", range: [8, 10], blurb: "Intermediate: graphical displays and interrupts." },
    { name: "Gold", range: [11, 12], blurb: "Advanced: touch control and connected systems." },
  ],
  sections: structured,
  stats: {
    paragraphs: paras.length,
    images: copiedImages.size,
    missingImages: [...missingImages],
  },
};

// ---------- write ----------
fs.mkdirSync(SITE_DATA_DIR, { recursive: true });
const outPath = path.join(SITE_DATA_DIR, "course.json");
fs.writeFileSync(outPath, JSON.stringify(course, null, 2));
console.log(`> Wrote ${path.relative(ROOT, outPath)}`);
console.log(`> Copied ${copiedImages.size} images to site/public/images/`);
if (missingImages.size) {
  console.warn(`! ${missingImages.size} image(s) referenced but not found in Media/:`);
  for (const m of missingImages) console.warn("    -", m);
}
