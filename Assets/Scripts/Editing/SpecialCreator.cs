using circleXsquares;
using UnityEngine;
using static EditGM;

public class SpecialCreator : MonoBehaviour
{
    // public read-accessibility state variables
    public HexOrient specOrient { get; private set; }
    public EditCreatorTool toolType { get; private set; }

    // private variables
    private EditGM _gmRef;
    private SnapCursor _anchorRef;

    private GameObject _checkpointToolRef;
    private GameObject _victoryToolRef;
    private GameObject _warpToolRef;

    void Awake()
    {
        // initialize internal variables
        specOrient = new HexOrient();
        toolType = EditCreatorTool.Checkpoint;

        _checkpointToolRef = transform.GetChild(0).gameObject;
        _victoryToolRef = transform.GetChild(1).gameObject;
        _warpToolRef = transform.GetChild(2).gameObject;
    }

    void Start()
    {
        // initialize reference to EditGM after it has awoken
        _gmRef = EditGM.instance;
        _anchorRef = _gmRef.anchorIcon;
    }

    void Update()
    {
        // when active, the special will follow the focus
        HexLocus f = _anchorRef.focus;
        int r = 0;
        int l = _gmRef.activeLayer;
        specOrient = new HexOrient(f, r, l);

        Quaternion q;
        transform.position = specOrient.ToUnitySpace(out q);
        transform.rotation = q;
    }

    // activates the appropriate special tool
    public void ActivateTool(EditCreatorTool inTool)
    {
        // set tool type
        toolType = inTool;

        // set relevant child to active
        _checkpointToolRef.SetActive(inTool == EditCreatorTool.Checkpoint);
        _victoryToolRef.SetActive(inTool == EditCreatorTool.Victory);
        _warpToolRef.SetActive(inTool == EditCreatorTool.Warp);
    }
}
