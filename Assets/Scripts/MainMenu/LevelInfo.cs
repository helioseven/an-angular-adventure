using System;

[Serializable]
public class LevelInfo
{
    public string id;              // Unique ID (could be local filename or Supabase row ID)
    public string name;            // Level name
    public bool isLocal;           // True if local draft
    public DateTime lastModified;  // Last edited/saved time
}

// LevelInfo FromSupabaseRow(Dictionary<string, object> row)
// {
//     return new LevelInfoappli
//     {
//         id = row["id"].ToString(),
//         name = row["name"].ToString(),
//         isLocal = false,
//         lastModified = DateTime.Parse(row["updated_at"].ToString())
//     };
// }
