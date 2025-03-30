using System;

[Serializable]
public class LevelInfo
{
    public string id; // Unique ID (could be local filename or Supabase row ID)
    public string name; // Level name
    public bool isLocal; // True if local draft
    public DateTime created_at; // Created at time
}
