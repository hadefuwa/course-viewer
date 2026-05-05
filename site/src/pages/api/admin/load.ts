import type { APIRoute } from "astro";
import { getCourse } from "../../../lib/supabase";

export const GET: APIRoute = async () => {
  const course = await getCourse();
  if (!course) {
    return new Response(JSON.stringify({ error: "Not found" }), { status: 404 });
  }
  return new Response(JSON.stringify(course), {
    headers: { "Content-Type": "application/json" },
  });
};
