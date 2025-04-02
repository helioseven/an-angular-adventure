using UnityEngine;

public class LevelNameGenerator : MonoBehaviour
{
    private static readonly string[] Adjectives = new string[]
    {
        "Crimson",
        "Silent",
        "Wobbly",
        "Gleaming",
        "Ancient",
        "Cozy",
        "Fractured",
        "Glorious",
        "Hidden",
        "Shimmering",
        "Twisted",
        "Echoing",
        "Floating",
        "Sunken",
        "Bouncy",
        "Rusty",
        "Acute",
        "Bent",
        "Fractal",
        "Twisted",
        "Oblique",
        "Pointed",
        "Segmented",
        "Skewed",
        "Spiraled",
        "Angular",
        "Concave",
        "Convex",
        "Radial",
        "Tessellated",
        "Winding",
        "Crisp",
    };

    private static readonly string[] Nouns = new string[]
    {
        "Meadow",
        "Labyrinth",
        "Orb",
        "Sanctum",
        "Tangle",
        "Echo",
        "Grove",
        "Puzzle",
        "Cascade",
        "Spire",
        "Fracture",
        "Nest",
        "Run",
        "Plateau",
        "Drift",
        "Chamber",
        "Angle",
        "Vertex",
        "Grid",
        "Prism",
        "Facet",
        "Tiling",
        "Vector",
        "Edge",
        "Path",
        "Plane",
        "Shape",
        "Spiral",
        "Tangent",
        "Pattern",
        "Array",
        "Corner",
    };

    public static string GenerateLevelName()
    {
        string adjective = Adjectives[Random.Range(0, Adjectives.Length)];
        string noun = Nouns[Random.Range(0, Nouns.Length)];
        return $"{adjective} {noun}";
    }
}
