using UnityEngine;

[CreateAssetMenu(fileName = "SupabaseConfig", menuName = "Config/SupabaseConfig")]
public class SupabaseConfig : ScriptableObject
{
    [Header("Edge Function")]
    public string functionUrl = "https://YOUR-REF.supabase.co/functions/v1/steam-partner";

    [Header("Auth Header")]
    [Tooltip("Usually the Supabase ANON key. Never store service role in builds.")]
    public string bearerToken = ""; // leave blank and load from env in Editor if you prefer

    [Header("DEV-only headers (Editor)")]
    public bool sendDevMockHeaders = true;
    public string devToken = ""; // set from env at runtime if you like
}
