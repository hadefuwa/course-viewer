# Admin Panel — Implementation Notes

> **Status: Implemented.** The admin panel is live at https://course-viewer-murex.vercel.app/admin
>
> The original plan below proposed a GitHub Contents API approach. The actual implementation used Supabase instead — simpler, no rebuild lag, edits are live immediately. This file is kept for historical reference.

---

## What Was Built

A PIN-protected content editor at `/admin` backed by Supabase.

### Architecture

```
Admin panel (browser)
  ├─ On login: GET /api/admin/load  ──→  Supabase course_content JSONB row
  ├─ User edits sections/blocks in the UI
  └─ Save: POST /api/admin/save  ──→  upsert to Supabase → pages reflect change immediately
```

No rebuild required — pages fetch from Supabase on every request, so saves are live instantly.

### Auth

- **PIN gate:** `4807` (course code). Stored in `sessionStorage`. Prevents accidental access — not a security guarantee.
- **Save authentication:** `ADMIN_SECRET` env var injected server-side into a `<meta>` tag on the admin page. The client reads it and sends it as an `x-admin-secret` header on every save request. The save endpoint validates it before writing to Supabase.

### Files

| File | Purpose |
|---|---|
| `site/src/pages/admin/index.astro` | Shell page — injects `ADMIN_SECRET` server-side, hosts the SPA |
| `site/src/pages/api/admin/load.ts` | `GET /api/admin/load` — returns course JSON from Supabase |
| `site/src/pages/api/admin/save.ts` | `POST /api/admin/save` — validates secret, upserts to Supabase |
| `site/src/lib/supabase.ts` | Supabase client + `getCourse()` helper used by all pages |

### Editing capabilities

- Worksheet title and hero image filename
- Section group headings
- Paragraph text, image src/alt, YouTube video ID
- Add / delete / reorder blocks within a section group
- Add / delete section groups

---

## Original Plan (superseded)

The original approach was to use the GitHub Contents API as a backend — edit `course.json` in the browser, commit it back to `main`, and let GitHub Actions rebuild the site (~90 seconds per save). This was rejected in favour of Supabase because:

- Supabase edits are live instantly (no rebuild delay)
- No GitHub PAT management required
- Cleaner separation: content in a database, not source control

The original GitHub API plan details are preserved below for reference.

---

### Original Architecture (not implemented)

```
Admin browser
  → GET github.com/api/...course.json   ← fetch + SHA
  → User edits in UI
  → PUT github.com/api/...course.json   ← commit to main
  → GitHub Actions fires automatically  ← rebuilds site (~90s)
```

### Original Auth Flow

1. **PIN gate** — hardcoded PIN stored in `sessionStorage`.
2. **GitHub PAT entry** — admin pastes a fine-grained PAT (scoped to `contents: write`). Validated via `GET /user`. Stored in `sessionStorage`.

### GitHub API Functions (not implemented)

| Function | API call |
|---|---|
| `fetchCourse(pat)` | `GET /repos/hadefuwa/course-viewer/contents/site/src/data/course.json` |
| `commitCourse(pat, sha, data, msg)` | `PUT /repos/.../contents/...` |
| `validatePat(pat)` | `GET /user` |

### Critical issue with original plan

The deploy workflow runs `extract.mjs` on every push, which **overwrites `course.json`** and erases admin edits. The proposed fix was an mtime guard in `extract.mjs`. With Supabase, this problem does not exist — the database is the source of truth, not the file.
