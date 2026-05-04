import { defineConfig } from "astro/config";

// GitHub Pages: site is served from /<repo-name>/
export default defineConfig({
  site: "https://hadefuwa.github.io",
  base: "/course-viewer",
  trailingSlash: "ignore",
  build: {
    assets: "_assets",
  },
});
