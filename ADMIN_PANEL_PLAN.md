# Admin Panel Implementation Plan

## Architecture

A client-side SPA at `/course-viewer/admin` — no new servers. It uses the **GitHub Contents API** as its backend: fetch `course.json`, edit it in the browser, commit back to `main`, which triggers the existing GitHub Actions deploy.

```
Admin browser
  → GET github.com/api/...course.json   ← fetch + SHA
  → User edits in UI
  → PUT github.com/api/...course.json   ← commit to main
  → GitHub Actions fires automatically  ← rebuilds site (~90s)
```

---

## New Files to Create

```
site/src/pages/admin/index.astro          ← shell page (noindex meta, loads SPA)
site/src/components/admin/
  types.ts          ← TypeScript interfaces matching course.json shape
  githubApi.ts      ← fetch/commit via GitHub REST API
  auth.ts           ← PIN gate + PAT validation
  editor.ts         ← in-memory course.json state + mutation functions
  ui.ts             ← DOM rendering for all three screens
  AdminApp.ts       ← orchestrator that wires everything together
site/public/admin-styles.css              ← scoped admin styles
```

---

## Auth Flow (2-Step)

1. **PIN gate** — hardcoded PIN (e.g. `4807`) stored in `sessionStorage`. Not real security — just a speed bump preventing accidental access. The PIN is visible in the JS bundle.
2. **GitHub PAT entry** — admin pastes a fine-grained GitHub PAT (scoped to `contents: write` on this repo only). Validated immediately via `GET /user`. Stored in `sessionStorage` (cleared on tab close).

---

## Three UI Screens

### Screen A — Worksheet List

Sidebar listing all sections (Cover, Preparation, Worksheets 1–12, Teacher Notes). Clicking one opens the editor.

### Screen B — Worksheet Editor

```
Title: [Connection points          ]
Hero Image: [connection_points.jpg ]
Tier: [Bronze ▾]

SECTIONS                           [+ Add Section]
┌─────────────────────────────────────────────────┐
│ Heading: [Overview         ]      [✕ Delete]    │
│ BLOCKS                            [+ Block]     │
│  ↕ paragraph  [text content here             ]  │
│  ↕ image      src: [img.jpg]  alt: [alt text ]  │
│  ↕ youtube    id:  [h8-7BsXBpLc              ]  │
└─────────────────────────────────────────────────┘
```

Blocks can be moved up/down, deleted, or added (paragraph / image / YouTube).

### Screen C — Save & Deploy Status

After committing: `✓ Saved to main. GitHub Actions rebuilding (~90s). [View workflow →]`

---

## GitHub API Integration (`githubApi.ts`)

| Function | API call | Notes |
|---|---|---|
| `fetchCourse(pat)` | `GET /repos/hadefuwa/course-viewer/contents/site/src/data/course.json` | Returns parsed JSON + SHA (SHA needed for PUT) |
| `commitCourse(pat, sha, data, msg)` | `PUT /repos/.../contents/...` | Commits base64-encoded JSON; message prefixed `admin:` |
| `validatePat(pat)` | `GET /user` | Returns `true` if 200 |

Non-ASCII chars require `btoa(unescape(encodeURIComponent(json)))` encoding to avoid `InvalidCharacterError`.

---

## Critical Fix Required — `scripts/extract.mjs`

**Problem:** The deploy workflow runs `extract.mjs` on every push, which **overwrites `course.json`** and erases admin edits.

**Fix:** Add an mtime guard at the top of `extract.mjs`:

```js
const docxMtime = fs.statSync('source/...docx').mtimeMs;
const jsonMtime = fs.existsSync('site/src/data/course.json')
  ? fs.statSync('site/src/data/course.json').mtimeMs : 0;
if (jsonMtime > docxMtime) {
  console.log('course.json is newer — skipping extract.');
  process.exit(0);
}
```

This means: if the admin edited `course.json` after the last `.docx` upload, extraction is skipped and admin edits survive the deploy.

---

## Risks & Mitigations

| Risk | Mitigation |
|---|---|
| PAT in sessionStorage | Use fine-grained PAT scoped to one repo only; revoke after sessions |
| Two admins editing simultaneously → 409 conflict | Handle 409 with "Reload to merge" prompt |
| `extract.mjs` overwrites admin edits | mtime guard above |
| Non-ASCII in JSON breaks `btoa()` | Use `btoa(unescape(encodeURIComponent(...)))` |

---

## Build Order

1. `types.ts` — everything depends on it
2. `githubApi.ts` — test in browser DevTools before building UI
3. `auth.ts`
4. `editor.ts`
5. `ui.ts` — worksheet list first, then block editor
6. `AdminApp.ts`
7. `site/src/pages/admin/index.astro` — shell page
8. `site/public/admin-styles.css`
9. Fix `scripts/extract.mjs` with mtime guard
10. Test locally, then push and verify at `/course-viewer/admin/`

---

## TypeScript Types (`types.ts`)

```typescript
export interface Block {
  type: 'p' | 'image' | 'youtube';
  text?: string;     // type === 'p'
  src?: string;      // type === 'image'
  alt?: string;      // type === 'image'
  url?: string;      // type === 'youtube'
  id?: string;       // type === 'youtube'
}

export interface SectionGroup {
  heading: string;
  blocks: Block[];
}

export interface CourseSection {
  kind: 'cover' | 'contents' | 'preparation' | 'worksheet' | 'teacher';
  number?: number;
  tier?: string;
  filename?: string;
  label?: string;
  title?: string;
  subtitle?: string;
  heroImage?: string | null;
  sections: SectionGroup[];
}

export interface CourseJson {
  code: string;
  title: string;
  unit: string;
  hours: number;
  publisher: string;
  source: string;
  generatedAt: string;
  tiers: { name: string; range: [number, number]; blurb: string }[];
  sections: CourseSection[];
}
```

---

## Editor State Functions (`editor.ts`)

```typescript
export function loadCourse(course: CourseJson): void
export function getCourse(): CourseJson
export function updateWorksheetMeta(sectionIndex: number, field: 'title' | 'heroImage' | 'tier', value: string): void
export function updateSectionHeading(sectionIndex: number, shIdx: number, value: string): void
export function updateBlock(sectionIndex: number, shIdx: number, blockIndex: number, field: string, value: string): void
export function moveBlock(sectionIndex: number, shIdx: number, blockIndex: number, direction: 'up' | 'down'): void
export function deleteBlock(sectionIndex: number, shIdx: number, blockIndex: number): void
export function addBlock(sectionIndex: number, shIdx: number, type: 'p' | 'image' | 'youtube'): void
export function addSection(sectionIndex: number): void
export function deleteSection(sectionIndex: number, shIdx: number): void
```
