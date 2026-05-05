// scripts/seed.mjs
// Push a course JSON file to Supabase course_content table.
//
// Usage:
//   node scripts/seed.mjs --key cp0539 --json site/src/data/cp0539.json
//
// Reads SUPABASE_URL and SUPABASE_SERVICE_KEY from site/.env automatically.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "..");

// ── Load site/.env ────────────────────────────────────────────────────────────
function loadDotenv(filePath) {
  if (!fs.existsSync(filePath)) return;
  const lines = fs.readFileSync(filePath, "utf8").split("\n");
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) continue;
    const eqIdx = trimmed.indexOf("=");
    if (eqIdx < 0) continue;
    const key = trimmed.slice(0, eqIdx).trim();
    const val = trimmed.slice(eqIdx + 1).trim().replace(/^["']|["']$/g, "");
    if (!process.env[key]) process.env[key] = val;
  }
}
loadDotenv(path.join(ROOT, "site", ".env"));

// ── Parse CLI args ────────────────────────────────────────────────────────────
const args = process.argv.slice(2);
function getArg(flag) {
  const i = args.indexOf(flag);
  return i >= 0 ? args[i + 1] : null;
}

const courseKey = getArg("--key");
const jsonArg = getArg("--json");

if (!courseKey || !jsonArg) {
  console.error("Usage: node scripts/seed.mjs --key <course-key> --json <path-to-json>");
  console.error("Example: node scripts/seed.mjs --key cp0539 --json site/src/data/cp0539.json");
  process.exit(1);
}

const jsonPath = path.resolve(ROOT, jsonArg);
if (!fs.existsSync(jsonPath)) {
  console.error(`JSON file not found: ${jsonPath}`);
  process.exit(1);
}

const SUPABASE_URL = process.env.SUPABASE_URL;
const SERVICE_KEY = process.env.SUPABASE_SERVICE_KEY;

if (!SUPABASE_URL || !SERVICE_KEY) {
  console.error("Missing SUPABASE_URL or SUPABASE_SERVICE_KEY — add them to site/.env");
  process.exit(1);
}

// ── Load and seed ─────────────────────────────────────────────────────────────
const courseData = JSON.parse(fs.readFileSync(jsonPath, "utf8"));
console.log(`> Seeding key="${courseKey}" from ${path.relative(ROOT, jsonPath)}`);
console.log(`  Title: ${courseData.title || "(no title)"}`);
console.log(`  Sections: ${(courseData.sections || []).length}`);

const url = `${SUPABASE_URL}/rest/v1/course_content?on_conflict=key`;
const res = await fetch(url, {
  method: "POST",
  headers: {
    "apikey": SERVICE_KEY,
    "Authorization": `Bearer ${SERVICE_KEY}`,
    "Content-Type": "application/json",
    "Prefer": "resolution=merge-duplicates",
  },
  body: JSON.stringify([{ key: courseKey, data: courseData, updated_at: new Date().toISOString() }]),
});

if (!res.ok) {
  const body = await res.text();
  console.error(`Supabase error ${res.status}: ${body}`);
  process.exit(1);
}

console.log(`> Successfully seeded "${courseKey}" to Supabase.`);
console.log("  The course will appear on the homepage at https://course-viewer-murex.vercel.app");
