# Vercel + Supabase Migration Plan

**Baseline tag:** `v1.0.0-static` — stable static GitHub Pages build before this migration.

## Goal

Replace the static JSON file (`site/src/data/course.json`) with a live Supabase database, deploy on Vercel, and wire the existing admin panel so course content can be edited through the UI and saved to the database.

## Architecture After Migration

```
Browser (Admin Panel)
  └─ POST /api/admin/save  ──→  Supabase (course_content JSONB row)
                                         ↑
Astro SSR pages  ──→  GET /api/admin/load ┘
  └─ Rendered on Vercel edge/serverless
```

## Database Schema (Supabase)

Single table — keeps it simple for the demo, no schema redesign needed:

```sql
create table course_content (
  id        serial primary key,
  key       text unique not null,   -- e.g. 'cp4807'
  data      jsonb not null,         -- full course.json blob
  updated_at timestamptz default now()
);
```

The entire `course.json` is stored as one JSONB row. This means the existing admin panel shape doesn't need to change.

Row Level Security: the anon key is read-only (public pages can fetch). Writes require the service role key (only available server-side in Astro API routes).

## Implementation Steps

### Step 1 — Vercel adapter + Supabase client
- Install `@astrojs/vercel` and `@supabase/supabase-js`
- Switch `astro.config.mjs` from static to `output: 'server'` with Vercel adapter
- Add `.env` with `SUPABASE_URL` and `SUPABASE_ANON_KEY` and `SUPABASE_SERVICE_KEY`

### Step 2 — Supabase helper
- Create `site/src/lib/supabase.ts` — thin client wrapper
- Seed initial data: run a one-off script that POSTs existing `course.json` into the `course_content` table

### Step 3 — API endpoints
- `site/src/pages/api/admin/load.ts` — GET, returns current `data` JSONB as JSON
- `site/src/pages/api/admin/save.ts` — POST (service key), receives JSON body, upserts row
- Both endpoints check a `ADMIN_SECRET` env var as a simple auth token (PIN stays in the UI)

### Step 4 — Wire admin panel
- On admin panel load: fetch `/api/admin/load` instead of importing course.json statically
- On Save button click: POST to `/api/admin/save` with current editor state
- Show success/error toast (UI skeleton already exists in admin panel)

### Step 5 — SSR pages fetch from Supabase
- Replace static `import courseData from '../data/course.json'` calls in each page with a server-side fetch to Supabase at request time
- Pages: `unit6.astro`, `worksheets/[number].astro`, `preparation.astro`, `teacher.astro`

### Step 6 — Deploy & environment variables
- Set all env vars in Vercel project settings
- Verify build passes, test admin save → page reload flow end to end

## Files Changed

| File | Change |
|---|---|
| `site/astro.config.mjs` | Add Vercel adapter, set `output: 'server'` |
| `site/package.json` | Add `@astrojs/vercel`, `@supabase/supabase-js` |
| `site/src/lib/supabase.ts` | New — Supabase client helper |
| `site/src/pages/api/admin/load.ts` | New — GET endpoint |
| `site/src/pages/api/admin/save.ts` | New — POST endpoint |
| `site/src/pages/admin/index.astro` | Wire save/load to API |
| `site/src/pages/unit6.astro` | Fetch from Supabase instead of JSON import |
| `site/src/pages/worksheets/[number].astro` | Fetch from Supabase instead of JSON import |
| `site/src/pages/preparation.astro` | Fetch from Supabase instead of JSON import |
| `site/src/pages/teacher.astro` | Fetch from Supabase instead of JSON import |

## Environment Variables Required

```
SUPABASE_URL=https://<project-ref>.supabase.co
SUPABASE_ANON_KEY=<anon-public-key>
SUPABASE_SERVICE_KEY=<service-role-secret-key>
ADMIN_SECRET=<choose-a-secret-token>
```

`ADMIN_SECRET` is sent as a header from the admin panel to authenticate save requests. Keep it out of version control.

## What Stays the Same

- The admin panel UI (no visual changes)
- The `course.json` data shape (stored as-is in JSONB)
- All page routes and URLs
- GitHub Actions deploy workflow is retired — Vercel handles CI/CD automatically on push to `main`

## Rollback

The tag `v1.0.0-static` can be deployed to GitHub Pages at any time from the Actions tab. The `course.json` file remains in the repo as a seed/backup.
