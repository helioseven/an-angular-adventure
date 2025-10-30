using System;

[Serializable]
public class LevelInfo
{
    public string id; // Unique ID (could be local filename or Supabase row ID)
    public string name; // Level name
    public bool isLocal; // True if local draft
    public string uploaderId; // corresponds to uploader_id from levels table in supabase db
    public DateTime createdAt; // Created at time
}
