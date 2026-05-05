import type { APIRoute } from "astro";
import { supabase, supabaseAdmin } from "../../../lib/supabase";

export const GET: APIRoute = async () => {
  const { data, error } = await supabase
    .from("course_content")
    .select("key, data->code, data->title, data->unit, data->hours, updated_at")
    .order("updated_at", { ascending: false });

  if (error) {
    return new Response(JSON.stringify({ error: error.message }), { status: 500 });
  }

  return new Response(JSON.stringify(data ?? []), {
    headers: { "Content-Type": "application/json" },
  });
};

export const POST: APIRoute = async ({ request }) => {
  const secret = request.headers.get("x-admin-secret");
  if (!secret || secret !== import.meta.env.ADMIN_SECRET) {
    return new Response(JSON.stringify({ error: "Unauthorized" }), { status: 401 });
  }

  let body: Record<string, unknown>;
  try {
    body = await request.json() as Record<string, unknown>;
  } catch {
    return new Response(JSON.stringify({ error: "Invalid JSON" }), { status: 400 });
  }

  const { key, data } = body as { key: string; data: unknown };
  if (!key || !data) {
    return new Response(JSON.stringify({ error: "key and data required" }), { status: 400 });
  }

  const { error } = await supabaseAdmin
    .from("course_content")
    .insert({ key, data, updated_at: new Date().toISOString() });

  if (error) {
    return new Response(JSON.stringify({ error: error.message }), { status: 500 });
  }

  return new Response(JSON.stringify({ ok: true }), {
    headers: { "Content-Type": "application/json" },
  });
};
