import { createClient } from "@supabase/supabase-js";

const url = import.meta.env.SUPABASE_URL as string;
const anon = import.meta.env.SUPABASE_ANON_KEY as string;
const service = import.meta.env.SUPABASE_SERVICE_KEY as string;

export const supabase = createClient(url, anon);
export const supabaseAdmin = createClient(url, service);

export async function getCourse() {
  const { data, error } = await supabase
    .from("course_content")
    .select("data")
    .eq("key", "cp4807")
    .single();
  if (error || !data) return null;
  return data.data as Record<string, any>;
}
