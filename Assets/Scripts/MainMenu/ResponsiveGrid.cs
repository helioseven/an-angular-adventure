using UnityEngine;
using UnityEngine.UI;

public class ResponsiveGrid : MonoBehaviour
{
    private const int MinColumns = 1;

    [SerializeField]
    private GridLayoutGroup grid;

    [SerializeField]
    private RectTransform viewport;

    [Header("Layout")]
    [SerializeField]
    private int maxColumns = 5;

    [SerializeField]
    private float targetCardWidth = 360f;

    [SerializeField]
    private float minCardWidth = 280f;

    [SerializeField]
    private float maxCardWidth = 420f;

    [SerializeField]
    private float cardHeight = 200f;

    [SerializeField]
    private float baseSpacing = 12f;

    [SerializeField]
    private int basePadding = 8;

    private float _lastScreenWidth = -1f;
    private float _lastViewportWidth = -1f;

    void Awake()
    {
        if (grid == null)
            grid = GetComponent<GridLayoutGroup>();
    }

    void LateUpdate()
    {
        if (viewport == null)
        {
            Debug.LogWarning("[ResponsiveGrid] Viewport is not assigned.");
            return;
        }

        float screenWidth = Screen.width;
        float viewportWidth = viewport.rect.width;

        if (
            Mathf.Abs(screenWidth - _lastScreenWidth) < 0.5f
            && Mathf.Abs(viewportWidth - _lastViewportWidth) < 0.5f
        )
        {
            return;
        }

        _lastScreenWidth = screenWidth;
        _lastViewportWidth = viewportWidth;

        if (grid == null || viewportWidth <= 0.1f)
            return;

        float availableForColumns = screenWidth - (basePadding * 2);
        if (availableForColumns <= (minCardWidth * 2f + baseSpacing))
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = MinColumns;
            grid.spacing = new Vector2(baseSpacing, grid.spacing.y);
            grid.padding.left = basePadding;
            grid.padding.right = basePadding;
            grid.cellSize = new Vector2(Mathf.Floor(availableForColumns), cardHeight);
            return;
        }
        float divisor = Mathf.Max(1f, targetCardWidth + baseSpacing);
        int safeColumns = Mathf.FloorToInt((availableForColumns + baseSpacing) / divisor);
        safeColumns = Mathf.Clamp(safeColumns, MinColumns, maxColumns);
        if (safeColumns < 1)
            safeColumns = MinColumns;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = safeColumns;
        grid.spacing = new Vector2(baseSpacing, grid.spacing.y);
        grid.padding.left = basePadding;
        grid.padding.right = basePadding;

        float available = viewportWidth - (basePadding * 2) - (baseSpacing * (safeColumns - 1));
        if (available <= 0f)
            return;

        float cellWidth = Mathf.Floor(available / safeColumns);
        if (cellWidth < minCardWidth)
        {
            while (safeColumns > 1 && cellWidth < minCardWidth)
            {
                safeColumns--;
                available = viewportWidth - (basePadding * 2) - (baseSpacing * (safeColumns - 1));
                cellWidth = Mathf.Floor(available / safeColumns);
            }
        }
        cellWidth = Mathf.Min(cellWidth, maxCardWidth);
        grid.cellSize = new Vector2(cellWidth, cardHeight);

        float used = (cellWidth * safeColumns) + (baseSpacing * (safeColumns - 1));
        float remaining = Mathf.Max(0f, viewportWidth - (basePadding * 2) - used);
        if (remaining > 0f)
        {
            float gap = remaining / (safeColumns + 1);
            grid.spacing = new Vector2(baseSpacing + gap, grid.spacing.y);
            int pad = Mathf.RoundToInt(basePadding + gap);
            grid.padding.left = pad;
            grid.padding.right = pad;
        }
    }
}
