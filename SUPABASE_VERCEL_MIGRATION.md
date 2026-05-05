# Vercel + Supabase Migration

**Status: Complete** — merged to `main` on 2026-05-05.

**Baseline tag:** `v1.0.0-static` — the static GitHub Pages build before this migration.

**Live URL:** https://course-viewer-murex.vercel.app

---

## What Changed

| Before | After |
|---|---|
| Static site (Astro `output: 'static'`) | SSR (Astro `output: 'server'`, Vercel adapter) |
| Content in `course.json` file | Content in Supabase `course_content` JSONB row |
| Admin save wrote to `localStorage` only | Admin save writes to Supabase, live immediately |
| Deployed to GitHub Pages (`/course-viewer/` path) | Deployed to Vercel (root path) |
| GitHub Actions CI/CD | Vercel auto-deploy on push to `main` |
| No base path in code possible | `base` config removed from `astro.config.mjs` |

---

## Database

**Supabase project:** `exwhlhofftsfmklyrugb`

**Table:**
```sql
create table course_content (
  id         serial primary key,
  key        text unique not null,
  data       jsonb not null,
  updated_at timestamptz default now()
);
alter table course_content enable row level security;
create policy public_read on course_content for select using (true);
```

Data was seeded from `site/src/data/course.json` on 2026-05-05.

---

## Files Changed

| File | Change |
|---|---|
| `site/astro.config.mjs` | Added Vercel adapter, `output: 'server'`, removed `base`/`site` |
| `site/package.json` | Added `@astrojs/vercel`, `@supabase/supabase-js`; upgraded Astro to v6 |
| `site/src/lib/supabase.ts` | New — Supabase client, `getCourse()` helper |
| `site/src/pages/api/admin/load.ts` | New — `GET /api/admin/load` |
| `site/src/pages/api/admin/save.ts` | New — `POST /api/admin/save` |
| `site/src/pages/admin/index.astro` | Injects `ADMIN_SECRET` server-side; save button calls real API |
| `site/src/pages/unit6.astro` | Replaced JSON import with `getCourse()` |
| `site/src/pages/worksheets/[number].astro` | Removed `getStaticPaths()`; SSR dynamic route via `Astro.params` |
| `site/src/pages/preparation.astro` | Replaced JSON import with `getCourse()` |
| `site/src/pages/teacher.astro` | Replaced JSON import with `getCourse()` |
| `site/.gitignore` | New — ignores `.env`, `dist/`, `.astro/` |

---

## Environment Variables

Set in Vercel project settings (`prj_xLw2SnhIwK3h1ya4BEKloiGnSNWv`):

| Variable | Environments |
|---|---|
| `SUPABASE_URL` | production, preview, development |
| `SUPABASE_ANON_KEY` | production, preview, development |
| `SUPABASE_SERVICE_KEY` | production, preview (sensitive); development (encrypted) |
| `ADMIN_SECRET` | production, preview (sensitive); development (encrypted) |

For local development, copy these into `site/.env` (gitignored).

---

## Rollback

The tag `v1.0.0-static` can be re-deployed to GitHub Pages via the Actions tab at any time. `course.json` remains in the repo as a backup of the seeded data.
