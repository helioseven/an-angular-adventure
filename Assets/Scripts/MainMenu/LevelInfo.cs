using System;

[Serializable]
public class LevelInfo
{
    public string id;              // Unique ID (could be local filename or Supabase row ID)
    public string name;            // Level name
    public bool isLocal;           // True if local draft
    public DateTime lastModified;  // Last edited/saved time
}