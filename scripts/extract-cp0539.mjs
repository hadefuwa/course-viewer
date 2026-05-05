// scripts/extract-cp0539.mjs
// Extract CP0539 — Industrial Maintenance of Closed Loop Systems
// from source/Unit 36 PLCs/ and write to site/src/data/cp0539.json
//
// This document uses Word heading styles (Heading2, Heading3) instead of the
// <worksheet> pseudo-XML markers used by CP4807. The parser here is adapted
// accordingly.
//
// Usage:  node scripts/extract-cp0539.mjs

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import AdmZip from "adm-zip";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "..");
const SOURCE_DIR = path.join(ROOT, "source", "Unit 36 PLCs");
const SHARED_MEDIA_DIR = path.join(ROOT, "source", "Media");
const SITE_DATA_DIR = path.join(ROOT, "site", "src", "data");
const SITE_IMG_DIR = path.join(ROOT, "site", "public", "images");

// ── Course metadata ──────────────────────────────────────────────────────────
const COURSE_META = {
  code: "CP0539",
  title: "Industrial Maintenance of Closed Loop Systems",
  unit: "BTEC Unit 36 — PLCs",
  hours: 60,
  subject: "Control Systems",
  publisher: "Matrix Technology Solutions",
};

const TIERS = [
  { name: "Bronze", range: [1, 5],  blurb: "Foundations: closed loop control, safety systems, PLC and HMI basics." },
  { name: "Silver", range: [6, 10], blurb: "Components: pump, valve and sensor operation and maintenance." },
  { name: "Gold",   range: [11, 15], blurb: "Advanced: sensor types, fault diagnosis and lockout/tagout procedures." },
];

const TIER_BY_NUMBER = {};
for (const t of TIERS) {
  for (let n = t.range[0]; n <= t.range[1]; n++) TIER_BY_NUMBER[n] = t.name;
}

// ── Locate the main docx (skip Answers file) ─────────────────────────────────
const allDocx = fs
  .readdirSync(SOURCE_DIR)
  .filter((f) => f.toLowerCase().endsWith(".docx") && !f.toLowerCase().includes("answer"));

if (!allDocx.length) {
  console.error(`No suitable .docx found in ${SOURCE_DIR}`);
  process.exit(1);
}
const docxName = allDocx[0];
const docxPath = path.join(SOURCE_DIR, docxName);
console.log(`> Reading ${docxName}`);

// ── Unzip in memory ───────────────────────────────────────────────────────────
const zip = new AdmZip(docxPath);
const readEntry = (name) => {
  const e = zip.getEntry(name);
  if (!e) throw new Error(`Missing entry in docx: ${name}`);
  return e.getData().toString("utf8");
};
const documentXml = readEntry("word/document.xml");
const relsXml = readEntry("word/_rels/document.xml.rels");

// ── rId → target map (distinguishes embedded vs. linked) ─────────────────────
// Embedded: Target="media/imageN.xxx"  → entry is at word/media/imageN.xxx in ZIP
// Linked:   Target="file:///some/abs/path/foo.jpg" → basename lives in source/Media/
const relsEmbedded = {}; // rId → ZIP entry name (word/media/...)
const relsLinked = {};   // rId → basename of external file

for (const m of relsXml.matchAll(
  /<Relationship\b[^>]*Id="([^"]+)"[^>]*Target="([^"]+)"[^>]*\/?>/g
)) {
  const id = m[1];
  const target = decodeURIComponent(m[2].replace(/\\/g, "/"));
  if (target.startsWith("media/") || target.startsWith("../media/")) {
    // Embedded image — resolve relative to word/
    relsEmbedded[id] = `word/${target.replace(/^\.\.\//, "")}`;
  } else {
    relsLinked[id] = target.split(/[\\/]/).pop();
  }
}

// ── Image extraction / resolution ────────────────────────────────────────────
fs.mkdirSync(SITE_IMG_DIR, { recursive: true });

const mediaKey = (s) => s.toLowerCase().replace(/[\s_\-]+/g, "_");
const normaliseName = (name) =>
  name.toLowerCase().replace(/\s+/g, "-").replace(/[^a-z0-9.\-_]/g, "");

// For external linked images (shared Media folder or beside the docx)
const sharedFiles = fs.existsSync(SHARED_MEDIA_DIR) ? fs.readdirSync(SHARED_MEDIA_DIR) : [];
const sharedIndex = new Map(sharedFiles.map((f) => [mediaKey(f), f]));
const localFiles = fs.readdirSync(SOURCE_DIR).filter((f) => /\.(png|jpe?g|gif|svg|webp)$/i.test(f));
const localIndex = new Map(localFiles.map((f) => [mediaKey(f), f]));

const copiedImages = new Map(); // rId → normalised output filename
const missingImages = new Set();

function resolveImageByRId(rId) {
  if (copiedImages.has(rId)) return copiedImages.get(rId);

  // Try embedded first
  const entryName = relsEmbedded[rId];
  if (entryName) {
    const entry = zip.getEntry(entryName);
    if (entry) {
      const ext = path.extname(entryName).toLowerCase();
      if ([".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"].includes(ext)) {
        // Use course-prefixed name to avoid collisions with CP4807 images
        const norm = `cp0539-${path.basename(entryName)}`;
        fs.writeFileSync(path.join(SITE_IMG_DIR, norm), entry.getData());
        copiedImages.set(rId, norm);
        return norm;
      }
    }
    missingImages.add(entryName);
    return null;
  }

  // Fall back to linked (external) image
  const basename = relsLinked[rId];
  if (!basename) return null;
  const key = mediaKey(basename);
  let srcDir = null;
  let actual = sharedIndex.get(key);
  if (actual) { srcDir = SHARED_MEDIA_DIR; }
  else { actual = localIndex.get(key); if (actual) srcDir = SOURCE_DIR; }
  if (!actual) { missingImages.add(basename); return null; }
  const ext = path.extname(actual).toLowerCase();
  if (![".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"].includes(ext)) return null;
  const norm = normaliseName(actual);
  fs.copyFileSync(path.join(srcDir, actual), path.join(SITE_IMG_DIR, norm));
  copiedImages.set(rId, norm);
  return norm;
}

// ── Parse paragraphs with style info ─────────────────────────────────────────
const decodeEntities = (s) =>
  s
    .replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"')
    .replace(/&apos;/g, "'").replace(/&amp;/g, "&")
    .replace(/[""]/g, '"').replace(/['']/g, "'");

function parseParagraphs(xml) {
  const out = [];
  const re = /<w:p(?:\s[^>]*)?>([\s\S]*?)<\/w:p>/g;
  let m;
  while ((m = re.exec(xml))) {
    const inner = m[1];
    const text = decodeEntities(
      [...inner.matchAll(/<w:t(?:\s[^>]*)?>([\s\S]*?)<\/w:t>/g)].map((mm) => mm[1]).join("")
    );
    const styleM = inner.match(/<w:pStyle\s+w:val="([^"]+)"/);
    const style = styleM ? styleM[1] : "Normal";
    const drawings = [];
    for (const dm of inner.matchAll(/<w:drawing\b[\s\S]*?<\/w:drawing>/g)) {
      for (const bm of dm[0].matchAll(/<a:blip\b[^>]*r:(?:link|embed)="([^"]+)"/g)) {
        drawings.push(bm[1]);
      }
    }
    out.push({ text: text.trim(), style, drawings });
  }
  return out;
}

const paras = parseParagraphs(documentXml);
console.log(`> ${paras.length} paragraphs`);

// ── State-machine parser ──────────────────────────────────────────────────────
// Structure:
//   Heading1 "Introduction"     → kind: "preparation"
//   Heading1 "Worksheets"       → separator (worksheets follow)
//   Heading2 "Worksheet N – …"  → new worksheet section
//   Heading3                    → new sub-section within current worksheet
//   Heading1 "Student Handbook" → kind: "teacher" (Q&A reference)
//   Heading1 "Safety Note"      → kind: "other"
//   Normal / ListParagraph      → content blocks
//   TOC* styles                 → skip (table of contents)

const isYouTube = (s) => /https?:\/\/(youtu\.be|(www\.)?youtube\.com)/i.test(s);
const ytId = (url) => {
  const m = url.match(/youtu\.be\/([A-Za-z0-9_-]{6,})/) || url.match(/[?&]v=([A-Za-z0-9_-]{6,})/);
  return m ? m[1].split(/[?&#]/)[0] : null;
};

const SKIP_STYLES = new Set(["TOC1", "TOC2", "TOC3", "TOCHeading"]);

// Sections we produce
const sections = [];
let currentSection = null;   // { kind, number?, tier?, title, sections: [], _curGroup }
let currentGroup = null;     // { heading, blocks }
let inWorksheetsRegion = false;

function flushGroup() {
  if (!currentGroup || !currentSection) return;
  if (currentGroup.blocks.length) currentSection.sections.push(currentGroup);
  currentGroup = null;
}

function flushSection() {
  flushGroup();
  if (!currentSection) return;
  if (currentSection.sections.length || currentSection.kind !== "other") {
    sections.push(currentSection);
  }
  currentSection = null;
}

function startGroup(heading) {
  flushGroup();
  currentGroup = { heading, blocks: [] };
}

function addBlock(block) {
  if (!currentSection) return;
  if (!currentGroup) startGroup("Overview");
  currentGroup.blocks.push(block);
}

function addImages(drawings) {
  for (const rId of drawings) {
    const local = resolveImageByRId(rId);
    const alt = relsLinked[rId] || relsEmbedded[rId] || rId;
    if (local) addBlock({ type: "image", src: local, alt });
  }
}

for (const p of paras) {
  const { text, style, drawings } = p;

  // Skip TOC lines
  if (SKIP_STYLES.has(style)) continue;

  // Always process drawings even on empty-text paragraphs
  if (style === "Heading1") {
    flushSection();
    inWorksheetsRegion = false;
    const label = text.toLowerCase();
    if (label === "worksheets") {
      inWorksheetsRegion = true;
      continue;
    }
    if (label === "introduction") {
      currentSection = { kind: "preparation", title: "Introduction", sections: [] };
      startGroup("Overview");
      continue;
    }
    if (label === "student handbook") {
      currentSection = { kind: "teacher", title: "Student Handbook", sections: [] };
      startGroup("Overview");
      continue;
    }
    if (label === "safety note") {
      currentSection = { kind: "other", title: "Safety Note", sections: [] };
      startGroup("Overview");
      continue;
    }
    if (label === "version control") {
      currentSection = { kind: "other", title: "Version Control", sections: [] };
      startGroup("Overview");
      continue;
    }
    // Any other Heading1
    currentSection = { kind: "other", title: text, sections: [] };
    startGroup("Overview");
    continue;
  }

  if (style === "Heading2" && inWorksheetsRegion) {
    flushSection();
    // "Worksheet N – Title" or "Worksheet N: Title"
    const m = text.match(/^Worksheet\s+(\d+)\s*[–\-:]\s*(.+)$/i);
    if (m) {
      const number = parseInt(m[1], 10);
      currentSection = {
        kind: "worksheet",
        number,
        tier: TIER_BY_NUMBER[number] || null,
        title: m[2].trim(),
        sections: [],
      };
    } else {
      currentSection = { kind: "worksheet", number: null, title: text, sections: [] };
    }
    startGroup("Overview");
    continue;
  }

  if (style === "Heading3") {
    // Skip "Student Handbook" sub-headings inside worksheets (just a reminder line)
    if (/^student\s+handbook$/i.test(text)) {
      flushGroup();
      currentGroup = { heading: "Student Handbook", blocks: [] };
      continue;
    }
    startGroup(text);
    continue;
  }

  // Treat Heading2 that appears outside the worksheets region as a plain heading
  if (style === "Heading2") {
    startGroup(text);
    continue;
  }

  // Content styles
  if (style === "Normal" || style === "ListParagraph" || style === "BodyText" ||
      style.startsWith("List") || style === "Quote") {
    addImages(drawings);
    if (!text) continue;
    if (isYouTube(text)) {
      addBlock({ type: "youtube", url: text, id: ytId(text) });
      continue;
    }
    // Prefix list items with a bullet
    const blockText = style === "ListParagraph" ? `• ${text}` : text;
    addBlock({ type: "p", text: blockText });
    continue;
  }

  // Fallback: any remaining style with text
  addImages(drawings);
  if (text) addBlock({ type: "p", text });
}

flushSection();

// ── Post-process: set heroImage on each worksheet ────────────────────────────
// Pulls the first image block from any group and promotes it to heroImage.
// The worksheet page uses skipImages to avoid rendering it twice inline.
for (const s of sections) {
  if (s.kind !== "worksheet") continue;
  for (const group of s.sections) {
    const imgBlock = group.blocks.find((b) => b.type === "image");
    if (imgBlock) { s.heroImage = imgBlock.src; break; }
  }
}

// ── Filter and log ────────────────────────────────────────────────────────────
const worksheets = sections.filter((s) => s.kind === "worksheet");
console.log(`> ${sections.length} total sections, ${worksheets.length} worksheets`);

// ── Build course doc ──────────────────────────────────────────────────────────
const course = {
  ...COURSE_META,
  source: docxName,
  generatedAt: new Date().toISOString(),
  tiers: TIERS,
  sections,
  stats: {
    paragraphs: paras.length,
    images: copiedImages.size,
    missingImages: [...missingImages],
  },
};

// ── Write ─────────────────────────────────────────────────────────────────────
fs.mkdirSync(SITE_DATA_DIR, { recursive: true });
const outPath = path.join(SITE_DATA_DIR, "cp0539.json");
fs.writeFileSync(outPath, JSON.stringify(course, null, 2));
console.log(`> Wrote ${path.relative(ROOT, outPath)}`);
console.log(`> Copied ${copiedImages.size} images`);

console.log("\nSection summary:");
for (const s of sections) {
  const groups = s.sections.length;
  const blocks = s.sections.reduce((a, g) => a + g.blocks.length, 0);
  if (s.kind === "worksheet") {
    console.log(`  WS ${s.number} [${s.tier || "no tier"}] "${s.title}" — ${groups} groups, ${blocks} blocks`);
  } else {
    console.log(`  [${s.kind}] "${s.title}" — ${groups} groups, ${blocks} blocks`);
  }
}
if (missingImages.size) {
  console.warn(`\n! ${missingImages.size} image(s) not found:`);
  for (const m of missingImages) console.warn("    -", m);
}
