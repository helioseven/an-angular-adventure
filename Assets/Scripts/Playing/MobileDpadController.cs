using circleXsquares;
using UnityEngine;
using UnityEngine.UI;

public class MobileDpadController : MonoBehaviour
{
    private const string LeftName = "Left";
    private const string RightName = "Right";
    private const string TopName = "Top";
    private const string BottomName = "Bottom";

    private DirectionControl _left;
    private DirectionControl _right;
    private DirectionControl _top;
    private DirectionControl _bottom;

    private void Awake()
    {
        CacheControls();
    }

    private void OnEnable()
    {
        if (PlayGM.instance != null)
            RefreshForGravity(PlayGM.instance.gravDirection);
    }

    public void RefreshForGravity(GravityDirection gravity)
    {
        CacheControls();

        bool horizontalActive =
            gravity == GravityDirection.Up || gravity == GravityDirection.Down;
        bool verticalActive =
            gravity == GravityDirection.Left || gravity == GravityDirection.Right;

        SetDirectionState(_left, horizontalActive);
        SetDirectionState(_right, horizontalActive);
        SetDirectionState(_top, verticalActive);
        SetDirectionState(_bottom, verticalActive);
    }

    private void CacheControls()
    {
        CacheDirection(ref _left, LeftName);
        CacheDirection(ref _right, RightName);
        CacheDirection(ref _top, TopName);
        CacheDirection(ref _bottom, BottomName);
    }

    private void CacheDirection(ref DirectionControl direction, string childName)
    {
        if (direction.IsValid)
            return;

        Transform child = transform.Find(childName);
        if (child == null)
            return;

        direction = new DirectionControl(
            child.GetComponent<Button>(),
            child.GetComponent<Graphic>()
        );
    }

    private static void SetDirectionState(DirectionControl direction, bool enabled)
    {
        if (direction.button != null)
            direction.button.interactable = enabled;

        if (direction.graphic != null)
            direction.graphic.raycastTarget = enabled;
    }

    private readonly struct DirectionControl
    {
        public readonly Button button;
        public readonly Graphic graphic;

        public bool IsValid => button != null || graphic != null;

        public DirectionControl(Button button, Graphic graphic)
        {
            this.button = button;
            this.graphic = graphic;
        }
    }
}
