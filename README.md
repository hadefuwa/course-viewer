# Unit 6 Microcontrollers — Course Viewer

A static site that presents Matrix TSL's **CP4807 — Introduction to Microcontrollers** course (BTEC Unit 6, 60-hour) as a beautiful course viewer for lecturers.

Live site: https://hadefuwa.github.io/course-viewer/

## Structure

```
source/                 # Original docx + image media (source of truth)
scripts/extract.mjs     # docx -> JSON + image normaliser
site/                   # Astro project (the viewer)
  src/data/course.json  # Generated
  src/assets/images/    # Generated (copied from source/Media)
.github/workflows/      # Build & deploy to GitHub Pages
```

## Local development

```bash
npm --prefix site install
node scripts/extract.mjs
npm --prefix site run dev
```

## Re-extracting after a docx update

Drop the new `.docx` into `source/`, update the filename in `scripts/extract.mjs` if needed, then:

```bash
node scripts/extract.mjs
```

The site rebuilds automatically on push.
