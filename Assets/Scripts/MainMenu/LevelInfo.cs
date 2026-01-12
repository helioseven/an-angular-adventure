using System;

[Serializable]
public class LevelInfo
{
    public string id; // Unique ID (could be local filename or Supabase row ID)
    public string name; // Level name
    public bool isLocal; // True if local draft
    public bool isBundled; // True if shipped with the build (read-only)
    public string uploaderId; // corresponds to uploader_id from levels table in supabase db
    public string uploaderDisplayName; // human friendly creator name from users table
    public DateTime createdAt; // Created at time
    public LevelPreviewDTO preview; // Optional base64 preview data
    public string dataHash; // Hash of level data lines (local/bundled only)
}
