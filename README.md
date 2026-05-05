# CP4807 — Course Viewer

A Matrix TSL course portal presenting **CP4807 — Introduction to Microcontrollers** (BTEC Unit 6, 60-hour) as an interactive viewer for lecturers, with a live admin panel backed by Supabase.

**Live site:** https://course-viewer-murex.vercel.app

**Admin panel:** https://course-viewer-murex.vercel.app/admin (PIN: `4807`)

---

## Repository structure

```
source/                        # Original .docx + image media (seed source)
scripts/extract.mjs            # .docx → course.json + image extractor
site/                          # Astro SSR project (deployed to Vercel)
  src/
    data/course.json           # Seed file — loaded into Supabase on first run
    lib/supabase.ts            # Supabase client + getCourse() helper
    pages/
      index.astro              # Portal homepage
      unit6.astro              # Microcontrollers course overview
      worksheets/[number].astro
      preparation.astro
      teacher.astro
      admin/index.astro        # PIN-protected content editor
      api/admin/
        load.ts                # GET  /api/admin/load
        save.ts                # POST /api/admin/save
  public/images/               # Course images (extracted from source)
.github/workflows/deploy.yml   # Legacy GitHub Pages workflow (no longer used)
SUPABASE_VERCEL_MIGRATION.md   # Migration log
ADMIN_PANEL_PLAN.md            # Original plan (superseded — see below)
```

---

## How it works

Pages are server-rendered on Vercel. On every request, `getCourse()` fetches the current course JSON from Supabase and renders the page. Edits made in the admin panel are written back to Supabase via `POST /api/admin/save` and appear immediately on the next page load.

```
Admin panel (browser)
  └─ POST /api/admin/save  ──→  Supabase course_content table
                                         ↑
Astro SSR pages  ──────────────  getCourse() on each request
  └─ Rendered on Vercel serverless
```

---

## Local development

1. Copy `.env.example` to `site/.env` and fill in the Supabase credentials (ask the project owner).
2. Install and run:

```bash
npm --prefix site install
npm --prefix site run dev
```

The dev server reads from the same Supabase project as production. To work offline or against a local seed, import `course.json` directly instead of using `getCourse()`.

---

## Re-extracting from a new .docx

Drop the updated `.docx` into `source/`, update the filename in `scripts/extract.mjs` if needed, then:

```bash
node scripts/extract.mjs
```

This regenerates `site/src/data/course.json`. To push the new content to Supabase, run the seed script or use the Supabase dashboard to upsert the row manually.

---

## Environment variables

| Variable | Where used | Notes |
|---|---|---|
| `SUPABASE_URL` | All pages | `https://<ref>.supabase.co` |
| `SUPABASE_ANON_KEY` | Public reads | JWT anon key |
| `SUPABASE_SERVICE_KEY` | Save endpoint only | Service role key — never expose client-side |
| `ADMIN_SECRET` | Save endpoint only | Arbitrary secret — sent as `x-admin-secret` header from admin panel |

Set in Vercel project settings (already configured). For local dev, add to `site/.env`.

---

## Deployment

Every push to `main` triggers an automatic Vercel build and deploy. No manual steps needed.
