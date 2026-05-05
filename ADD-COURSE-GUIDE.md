# Adding a New Course — Step-by-Step Guide

This guide documents how to add a new BTEC course to the course viewer. Follow these steps exactly and you can add any course from a Word document in under 30 minutes.

---

## Overview

The pipeline is:

```
source/Unit XX/<CourseName>.docx
        ↓  (extract script)
site/src/data/<code>.json  +  site/public/images/
        ↓  (seed script)
Supabase course_content table (key = <code>)
        ↓  (auto)
Homepage course grid  +  /courses/<code>/worksheets/N
```

Once data is in Supabase the course appears on the live site automatically — no page code changes needed.

---

## Step 1 — Create the source folder

Create a folder under `source/` for the new unit:

```
source/
  Unit 36 PLCs/           ← example (already done)
  Unit XX <Subject>/      ← your new folder
    CP0000 - Course Title.docx
```

If the document has externally-linked images (like CP4807), put them in `source/Media/`.  
If images are embedded inside the `.docx` itself (like CP0539), no extra folder is needed.

---

## Step 2 — Create an extraction script

Copy the CP0539 script as a template:

```
scripts/extract-cp0539.mjs  →  scripts/extract-cp0000.mjs
```

Then edit the new script. You only need to change these sections at the top:

### 2a — SOURCE_DIR

```js
const SOURCE_DIR = path.join(ROOT, "source", "Unit XX <Subject>");
```

### 2b — COURSE_META

```js
const COURSE_META = {
  code: "CP0000",
  title: "Your Course Title",
  unit: "BTEC Unit XX — Subject Name",
  hours: 60,           // adjust to actual contact hours
  subject: "Subject",  // shown on the homepage card
  publisher: "Matrix Technology Solutions",
};
```

### 2c — TIERS

Adjust ranges to match the number of worksheets in the document:

```js
const TIERS = [
  { name: "Bronze", range: [1, 5],  blurb: "Short description of bronze content." },
  { name: "Silver", range: [6, 10], blurb: "Short description of silver content." },
  { name: "Gold",   range: [11, 15], blurb: "Short description of gold content." },
];
```

You can change these ranges via the admin panel later without re-running the script.

### 2d — SUBTITLE_RE

Update the subtitle pattern to match the recurring header text in the new document:

```js
// CP0539 example:
const SUBTITLE_RE = /industrial\s+maintenance|closed[\s\-]loop\s+systems|CP0539/i;

// Change to match your course — e.g., for a thermodynamics course:
const SUBTITLE_RE = /thermodynamics|heat\s+engines|CP1234/i;
```

### 2e — Document format

The extract script supports two document formats:

| Format | How detected | Used by |
|---|---|---|
| **Heading-based** | Worksheets are `Heading2` paragraphs | CP0539 and newer docs |
| **Marker-based** | Worksheets wrapped in `<worksheet>…</worksheet>` text | CP4807 (original) |

The CP0539 script (`extract-cp0539.mjs`) handles the heading-based format.  
If a new document uses `<worksheet>` markers instead, use `extract.mjs` as the template.

**How to tell which format a document uses:** Run a quick check:
```bash
node -e "
import('adm-zip').then(({default:AdmZip}) => {
  const zip = new AdmZip('source/Unit XX/CourseName.docx');
  const xml = zip.getEntry('word/document.xml').getData().toString('utf8');
  console.log('Has worksheet markers:', /<worksheet>/i.test(xml));
  const re = /<w:p[\s\S]*?<\/w:p>/g;
  let m, found = 0;
  while ((m = re.exec(xml)) && found < 5) {
    const s = m[0].match(/<w:pStyle\s+w:val=\"([^\"]+)\"/);
    const t = [...m[0].matchAll(/<w:t[^>]*>([\s\S]*?)<\/w:t>/g)].map(x=>x[1]).join('');
    if (s && t.trim()) { console.log('['+s[1]+']', t.trim().slice(0,80)); found++; }
  }
});
"
```

### 2f — Output filename

Change the output path so it doesn't overwrite other courses:

```js
const outPath = path.join(SITE_DATA_DIR, "cp0000.json");  // use your course code
```

---

## Step 3 — Add an npm script

In `package.json`, add an entry to the `scripts` block:

```json
"extract:cp0000": "node scripts/extract-cp0000.mjs"
```

---

## Step 4 — Run the extraction

```bash
npm run extract:cp0000
```

Check the console output. You should see:
- Paragraph count (confirms the file was read)
- Section/worksheet count
- Images copied
- A summary table listing every worksheet with its tier and block count

If worksheets come back with 0 blocks, the heading styles in that document may differ — run the diagnostic from Step 2e to inspect the actual paragraph styles.

**Tip:** After running, open `site/src/data/cp0000.json` and verify a worksheet looks sensible. Check the `sections` array inside any worksheet entry — each group should have meaningful `heading` and `blocks` content.

---

## Step 5 — Seed to Supabase

```bash
node scripts/seed.mjs --key cp0000 --json site/src/data/cp0000.json
```

The `--key` must be the lowercase course code (e.g. `cp0539`). This is the Supabase row key and forms part of the URL (`/courses/cp0000`).

The seed script reads credentials from `site/.env` automatically.

If you need to re-seed after making changes, just run the same command again — it uses an upsert so it's safe to repeat.

---

## Step 6 — Check the homepage (no deploy needed)

The homepage fetches courses from Supabase on every request (server-side rendering). As soon as the seed script succeeds, the course card is live — **no git push or Vercel deploy required** for this step.

Visit https://course-viewer-murex.vercel.app and confirm:

- A new card for your course appears in the Course Library grid
- It shows the correct title, unit, code, hours, worksheet count, and tier badges
- It links to `/courses/cp0000`

If the card doesn't appear, check that the seed completed without errors and that `SUPABASE_URL`/`SUPABASE_SERVICE_KEY` in `site/.env` are correct.

---

## Step 7 — Commit and push so images load on Vercel

The JSON lives in Supabase (already live), but **images are static files** served by Vercel. Until you push them, the course card and worksheet pages will render without any photos.

```bash
git add scripts/extract-cp0000.mjs site/src/data/cp0000.json site/public/images/
git commit -m "Add CP0000 — Course Title"
git push
```

Vercel auto-deploys on push to `main`. Once the deploy completes (usually under 60 seconds):

- The thumbnail on the homepage card will appear
- Hero images on each worksheet page will appear
- All inline images within worksheet content will appear

You can watch the deploy at https://vercel.com/hamed-adefuwa-s-projects/course-viewer

---

## Step 8 — Fine-tune via the Admin Panel

Visit `/admin` (PIN: `4807`) to make any adjustments without re-running the scripts:

- Edit worksheet titles
- Adjust tier ranges
- Edit or delete individual blocks
- Add YouTube video blocks
- Reorder content groups

Changes save live to Supabase and take effect immediately.

---

## Troubleshooting

### No worksheets found (0 sections)
The document doesn't use `Heading2` for worksheet titles. Check actual styles with the diagnostic in Step 2e and adjust the parser accordingly.

### Images not showing
- Embedded images: they must be committed to `site/public/images/` (they're extracted during the script run, check they appear there)
- Externally-linked images: add them to `source/Media/` before running the extract script

### Wrong worksheet count or tier assignment
Edit `TIERS` in your extract script and re-run, or use the admin panel to adjust individual worksheets.

### Supabase error on seed
Check `site/.env` has `SUPABASE_URL` and `SUPABASE_SERVICE_KEY` set correctly. Run `cat site/.env` to verify.

---

## Files created per new course

| File | Purpose |
|---|---|
| `scripts/extract-cp0000.mjs` | Extraction script (commit to repo) |
| `site/src/data/cp0000.json` | Local seed/backup JSON (commit to repo) |
| `site/public/images/cp0000-*.png/jpg` | Extracted images (commit to repo) |
| Supabase row `key='cp0000'` | Live data source (seeded, not a file) |

The generic routes `/courses/[key]` and `/courses/[key]/worksheets/[number]` handle the course automatically — no new Astro pages needed.

---

## CP0539 as a reference implementation

The full working example is CP0539 — Industrial Maintenance of Closed Loop Systems:

- Extract script: `scripts/extract-cp0539.mjs`
- Seed JSON: `site/src/data/cp0539.json`
- Images: `site/public/images/cp0539-image*.png/jpeg`
- Supabase key: `cp0539`
- Live URL: `https://course-viewer-murex.vercel.app/courses/cp0539`
