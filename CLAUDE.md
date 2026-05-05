# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Purpose

This is a **Vercel-hosted course portal** for Matrix TSL's BTEC engineering courses. The site has two layers:

1. **Portal homepage** — a Matrix TSL branded landing page listing all available and coming-soon courses.
2. **Course viewer** — an interactive viewer for the 60-hour BTEC Unit 6 Microcontrollers course (CP4807).
3. **Admin panel** — a PIN-protected content editor that saves live to Supabase.

The audience is **lecturers** evaluating what Matrix TSL offers — it should look polished and professional.

The original course content came from a poorly-formatted Word document from a manager, containing course content mixed with AI authoring instructions and pseudo-XML metatags. The goal was to extract the real course content, strip the noise, and present it as a clean browsable website.

## Infrastructure

| Layer | Service |
|---|---|
| Hosting / SSR | Vercel (auto-deploys on push to `main`) |
| Database | Supabase (PostgreSQL with JSONB column) |
| Build framework | Astro v6, `output: "server"` |

**Live URL:** https://course-viewer-murex.vercel.app

**Always push to `main`.** Vercel auto-deploys on every push. The old GitHub Actions workflow (`.github/workflows/deploy.yml`) is no longer active.

## Source Document Problem

The `.docx` in `source/` is a Word file used as an AI authoring template — it contains:
- Actual course content (worksheets, tasks, challenges, teacher notes)
- AI instructions mixed into the text (these should be ignored/stripped)
- Pseudo-XML markers (`<worksheet>`, `<filename>`, `<image>`) used as delimiters
- **Images that are NOT embedded** — they live in `source/Media/` and are externally linked

The fix: `scripts/extract.mjs` unzips the `.docx` (Word files are ZIP archives), parses `word/document.xml`, uses the pseudo-XML markers to split content into sections, and copies images from `source/Media/` into `site/public/images/`.

## Data Pipeline

```
source/*.docx  →  scripts/extract.mjs  →  site/src/data/course.json  (seed file)
source/Media/  →  scripts/extract.mjs  →  site/public/images/

Supabase: course_content table (JSONB row, key = 'cp4807')
  ↑ seeded once from course.json
  ↑ updated live via admin panel → POST /api/admin/save

Astro SSR pages  →  getCourse() fetches Supabase on every request  →  Vercel
```

`course.json` in the repo is a seed/backup. The live source of truth is the Supabase row. Editing via admin panel updates Supabase and pages reflect it immediately.

## Course Structure

The course has three attainment tiers: **Bronze**, **Silver**, **Gold**. Content is split into:

| Section | Description |
|---|---|
| Cover | Course overview and metadata |
| Preparation | Pre-course setup for students |
| Worksheets 1–12 | Main course content, one page each |
| Teacher Notes | Lecturer-only reference material |

Each worksheet page groups content under headings like "Over to you:", "Challenges:", etc.

## `course.json` / Supabase Data Shape

The JSONB column in Supabase stores this structure verbatim:

```json
{
  "code": "CP4807",
  "title": "Introduction to Microcontrollers",
  "unit": "BTEC Unit 6 — Microcontrollers",
  "hours": 60,
  "publisher": "Matrix Technology Solutions",
  "tiers": [
    { "name": "Bronze", "range": [1, 7], "blurb": "..." },
    { "name": "Silver", "range": [8, 10], "blurb": "..." },
    { "name": "Gold",   "range": [11, 12], "blurb": "..." }
  ],
  "sections": [
    {
      "kind": "cover" | "contents" | "preparation" | "worksheet" | "teacher",
      "number": 1,
      "tier": "Bronze",
      "title": "First program",
      "heroImage": "flashing_light.jpg",
      "sections": [
        {
          "heading": "Overview",
          "blocks": [
            { "type": "p", "text": "..." },
            { "type": "image", "src": "file.jpg", "alt": "..." },
            { "type": "youtube", "url": "https://youtu.be/...", "id": "..." }
          ]
        }
      ]
    }
  ]
}
```

`sections` is a flat array of 15 items: cover, contents, preparation, 12 worksheets, teacher notes.

## Supabase Schema

```sql
create table course_content (
  id         serial primary key,
  key        text unique not null,   -- 'cp4807'
  data       jsonb not null,         -- full course JSON blob
  updated_at timestamptz default now()
);
alter table course_content enable row level security;
create policy public_read on course_content for select using (true);
```

Row Level Security: anon key = public read only. Writes require the service role key, which only exists server-side in the save endpoint.

## Environment Variables

| Variable | Use |
|---|---|
| `SUPABASE_URL` | Supabase project URL |
| `SUPABASE_ANON_KEY` | Public read access |
| `SUPABASE_SERVICE_KEY` | Write access — save endpoint only, never client-side |
| `ADMIN_SECRET` | Sent as `x-admin-secret` header from admin panel to authenticate saves |

All set in Vercel project settings. For local dev, add to `site/.env` (gitignored).

## Site Structure

### Layouts
- **`site/src/layouts/HomeLayout.astro`** — layout for the portal homepage; Matrix TSL logo, branded header/footer. Contains an **Admin** link in the top-right nav (`hp-admin-link` class).
- **`site/src/layouts/Base.astro`** — layout for all course pages; includes "← All Courses" link back to the portal. Contains an **Admin** link in the top-right nav (`admin-nav-link` class).

### Pages
- **`site/src/pages/index.astro`** — **portal homepage**: Matrix TSL branding, animated ticker, hero with stats, course library grid (1 live + 4 coming soon)
- **`site/src/pages/unit6.astro`** — **course overview**: worksheet grid grouped by Bronze/Silver/Gold tier
- **`site/src/pages/worksheets/[number].astro`** — one page per worksheet (dynamic SSR route); cinematic hero, XP/progress system, task checkboxes, challenge cards, hints, notes
- **`site/src/pages/preparation.astro`** — pre-course setup page
- **`site/src/pages/teacher.astro`** — lecturer-only reference notes
- **`site/src/pages/admin/index.astro`** — **admin panel** (see Admin Panel section below); PIN-protected, `noindex`
- **`site/src/pages/api/admin/load.ts`** — `GET /api/admin/load` — returns current course JSON from Supabase
- **`site/src/pages/api/admin/save.ts`** — `POST /api/admin/save` — upserts course JSON to Supabase (requires `x-admin-secret` header)

### Supabase Helper
- **`site/src/lib/supabase.ts`** — exports `supabase` (anon client), `supabaseAdmin` (service role client), and `getCourse()` (convenience fetch). All pages call `getCourse()` in their frontmatter.

### Components & Styles
- **`site/src/components/Section.astro`** — renders content blocks; special layouts for "Over to you" (tasks), "Challenge", and "Hint" sections
- **`site/src/styles/global.css`** — all styles using CSS custom properties; no CSS framework. Homepage styles (`hp-` prefix) live as `<style is:global>` inside `index.astro`.

### Assets
- **`site/public/matrix-logo.svg`** — Matrix TSL white SVG logo
- **`site/public/images/`** — course images (extracted from source) + course thumbnail images

## Routing

No base path prefix — the site is at the root on Vercel.

| URL | Page |
|---|---|
| `/` | Portal homepage |
| `/unit6` | Microcontrollers course overview |
| `/worksheets/[n]` | Individual worksheet (1–12) |
| `/preparation` | Preparation page |
| `/teacher` | Teacher notes |
| `/admin` | Admin panel (PIN `4807`) |
| `/api/admin/load` | GET — fetch course data |
| `/api/admin/save` | POST — save course data |

## Design System

Dark theme throughout. CSS custom properties defined in `:root` in `global.css`:

| Variable | Value | Use |
|---|---|---|
| `--bg` | `#0b1220` | Page background |
| `--bg-card` | `#16213d` | Card backgrounds |
| `--accent` | `#ff6b1f` | Orange — course pages primary accent |
| `--accent-2` | `#4dd0ff` | Cyan — task/interactive elements |
| `--gold` | `#ffd34d` | Challenge sections |

The portal homepage uses its own palette (`hp-` prefixed classes in `index.astro`):
- Background: `#080c15` with CSS grid-line pattern
- Primary accent: `#9333ea` / `#a855f7` (purple — matches Matrix TSL logo mark)

## Course Cards on Homepage

Courses are hardcoded in `index.astro`. To add a new live course:
1. Add an entry to the `courses` array with `live: true` and a `href` pointing to its overview page
2. Add a `thumb` image path (copy the image to `site/public/images/`)
3. Create the course's pages under `site/src/pages/`

Coming-soon courses need only `unit`, `title`, `subject`, `hours`, `live: false`, and `thumb`.

## Admin Panel

A PIN-protected content editor at `/admin`. It is a server-rendered page (the `ADMIN_SECRET` is injected server-side via a `<meta>` tag, never bundled in JS) wrapping a fully client-side SPA editor.

### Access
- **URL:** `/admin` — linked from the **Admin** button in the top-right nav on every page
- **PIN:** `4807` (the course code). Auth is stored in `sessionStorage` — clears when the browser tab closes.

### How it works
1. On login, the editor loads course data from `localStorage` if present (fast), then `GET /api/admin/load` to get the live Supabase data.
2. Admin edits sections, groups, and blocks in the UI.
3. **Save** posts the full course JSON to `POST /api/admin/save` with the `x-admin-secret` header. The endpoint validates the secret and upserts to Supabase. Pages reflect the change on their next request.

### Editing capabilities
- Edit worksheet title and hero image filename
- Edit section group headings
- Edit paragraph text (transparent textarea embedded in card)
- Edit image filename and alt text
- Edit YouTube video ID
- Add / delete / reorder blocks within a section group
- Add / delete section groups within a worksheet

### Block types
| Type | Fields |
|---|---|
| `p` | `text` |
| `image` | `src`, `alt` |
| `youtube` | `id`, `url` |

### Security model
- `ADMIN_SECRET` is an environment variable injected at request time into a `<meta>` tag — it is never in the built JS bundle and not visible in the static page source of non-admin pages.
- The service role key (`SUPABASE_SERVICE_KEY`) only exists in the Vercel serverless environment — it is never sent to the browser.
- The PIN (`4807`) is a lightweight access control, not a security guarantee. It prevents accidental access, not a determined attacker.

## Astro CSS Scoping — Critical Gotcha

**Problem:** Astro's `<style>` blocks are scoped by default. Astro adds a unique hash attribute (e.g. `data-astro-cid-xxxxxx`) to every selector AND to every static HTML element in the template. Elements created dynamically via JavaScript (`innerHTML`, `createElement`, etc.) never receive this attribute, so **all CSS rules silently fail to apply** to them.

**Symptom:** Styles work on static HTML but have no effect on anything rendered by JS — layouts look unstyled or revert to browser defaults despite the CSS being present in the file.

**Fix:** Any page or component that renders its UI dynamically via JavaScript must use `<style is:global>` instead of `<style>`.

```astro
<!-- ✗ Wrong — JS-created elements won't receive the scoping attribute -->
<style>
  .block-card { ... }
</style>

<!-- ✓ Correct — styles apply to all elements regardless of how they were created -->
<style is:global>
  .block-card { ... }
</style>
```

This applies to the admin panel (`site/src/pages/admin/index.astro`) and any future pages that build their DOM in JavaScript.
