import type { APIRoute } from "astro";
import { supabase } from "../../../lib/supabase";

export const GET: APIRoute = async ({ url }) => {
  const key = url.searchParams.get("key") || "cp4807";
  const { data, error } = await supabase
    .from("course_content")
    .select("data")
    .eq("key", key)
    .single();
  if (error || !data) {
    return new Response(JSON.stringify({ error: "Not found" }), { status: 404 });
  }
  return new Response(JSON.stringify(data.data), {
    headers: { "Content-Type": "application/json" },
  });
};
