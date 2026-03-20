using circleXsquares;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectInfoControl : MonoBehaviour
{
    /* Private References */
    private EditGM _editGM;
    private TileCreator _tileCreator;
    private SpecialCreator _checkpointCreator;
    private SpecialCreator _warpCreator;
    private SpecialCreator _victoryCreator;
    private TMP_Text _colorDisplay;
    private InfoPack _lastFrameInfoPack;
    private TMP_Text _locusDisplay;
    private Image _objectDisplay;
    private AspectRatioFitter _objectDisplayARF;
    private TMP_Text _rotationDisplay;
    private TMP_Text _specialLabel;
    private TMP_Text _specialDisplay;
    private TMP_Text _doorIDLabel;
    private TMP_Text _doorIDDisplay;
    private TMP_Text _typeDisplay;
    private TMP_Text _combinedNameDisplay;
    private TMP_InputField _doorIdInputField;
    private TMP_InputField _specialInputField;
    private Button _decreaseDoorIdButton;
    private Button _increaseDoorIdButton;
    private Button _decreaseSpecialButton;
    private Button _increaseSpecialButton;

    /* Private Variables */

    private bool _isAnyItemSelected;
    private bool _isInstanceNull;
    private Transform _standardAttributesPanel;
    private Transform _doorIdPanel;
    private Transform _specialPanel;

    private readonly float[] _aspectRatios = new float[] { 1f, 2f, 2f, 1f, 1f, 2f };
    private readonly string[] _colorStrings = new string[]
    {
        "Black",
        "Blue",
        "Brown",
        "Green",
        "Orange",
        "Purple",
        "Red",
        "White",
    };
    private readonly string[] _typeStrings = new string[]
    {
        "Triangle",
        "Diamond",
        "Trapezoid",
        "Hexagon",
        "Square",
        "Wedge",
        "Checkpoint",
        "Warp",
    };

    void Awake()
    {
        _isAnyItemSelected = false;
        _lastFrameInfoPack = new InfoPack();

        // turn off the special number (only green/orange have this value but black tri is default)
        transform.GetChild(3).gameObject.SetActive(false);
    }

    void Start()
    {
        _editGM = EditGM.instance;
        _tileCreator = _editGM.tileCreator;
        _checkpointCreator = _editGM.checkpointTool.GetComponent<SpecialCreator>();
        _warpCreator = _editGM.warpTool.GetComponent<SpecialCreator>();
        _victoryCreator = _editGM.victoryTool.GetComponent<SpecialCreator>();

        _objectDisplay = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        _objectDisplayARF = _objectDisplay.GetComponent<AspectRatioFitter>();

        _combinedNameDisplay = transform.GetChild(4).GetComponent<TMP_Text>();

        Transform t = transform.GetChild(1);
        _standardAttributesPanel = t;
        _typeDisplay = FindText(t, "Type Display");
        _colorDisplay = FindText(t, "Color Display");
        _rotationDisplay = FindText(t, "Rotation Display");
        _locusDisplay = FindText(t, "Position Display");

        t = transform.GetChild(3);
        _specialPanel = t;
        _specialLabel = FindText(t, "Special Label");
        _specialDisplay = FindText(t, "Special Display");
        _specialInputField = t.GetComponentInChildren<TMP_InputField>(true);
        CachePanelButtons(t, out _decreaseSpecialButton, out _increaseSpecialButton);

        t = transform.GetChild(2);
        _doorIdPanel = t;
        _doorIDLabel = FindText(t, "DoorID Label");
        _doorIDDisplay = FindText(t, "DoorID Display");
        _doorIdInputField = t.GetComponentInChildren<TMP_InputField>(true);
        CachePanelButtons(t, out _decreaseDoorIdButton, out _increaseDoorIdButton);

        BindRuntimeListeners();
    }

    void Update()
    {
        InfoPack infoPack = getUpdatedInfo();

        if (_lastFrameInfoPack != infoPack)
            updateUI(infoPack);

        _lastFrameInfoPack = infoPack;
    }

    /* Private Structs */

    // a struct for reporting what current display information should be
    private struct InfoPack
    {
        public int type;
        public int color;
        public int spec;
        public int rot;
        public HexLocus locus;
        public int doorID;

        public InfoPack(
            int inType,
            int inColor,
            int inSpec,
            int inRot,
            HexLocus inLocus,
            int inDoorID
        )
        {
            type = inType;
            color = inColor;
            spec = inSpec;
            rot = inRot;
            locus = inLocus;
            doorID = inDoorID;
        }

        public static bool operator ==(InfoPack ip1, InfoPack ip2)
        {
            return (
                (ip1.type == ip2.type)
                && (ip1.color == ip2.color)
                && (ip1.spec == ip2.spec)
                && (ip1.rot == ip2.rot)
                && (ip1.locus == ip2.locus)
                && (ip1.doorID == ip2.doorID)
            );
        }

        public static bool operator !=(InfoPack ip1, InfoPack ip2)
        {
            return !(ip1 == ip2);
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override bool Equals(System.Object obj)
        {
            InfoPack? infoPack = obj as InfoPack?;
            if (!infoPack.HasValue)
                return false;
            else
                return this == infoPack.Value;
        }

        // .NET expects this behavior to be overridden when overriding ==/!= operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /* Private Functions */

    // gets information from appropriate sources to fill InfoPack
    private InfoPack getUpdatedInfo()
    {
        EditGM.SelectedItem selectedItem = _editGM.selectedItem;
        _isAnyItemSelected = selectedItem != EditGM.SelectedItem.noSelection;
        _isInstanceNull = selectedItem.instance == null;

        // Set Infopack return items to their default values
        // type should always be updated during this function
        int type = -1;

        // the rest change for tiles but stay as default negative 1 for special tiles
        int color = -1;
        int spec = -1;
        int rot = -1;
        HexLocus locus = new HexLocus();
        int doorID = -1;

        // If the editor is in Create mode
        //   update object panel InfoPack based on active and enabled CreatorTool status
        if (_editGM.isEditorInCreateMode)
        {
            switch (_editGM.currentCreatorTool)
            {
                case EditGM.EditCreatorTool.Tile:
                    type = _tileCreator.tileType;
                    color = _tileCreator.tileColor;
                    spec = _tileCreator.tileSpecial;
                    rot = _tileCreator.tileOrient.rotation;
                    locus = _tileCreator.tileOrient.locus;
                    doorID = _tileCreator.tileDoorID;
                    break;
                case EditGM.EditCreatorTool.Checkpoint:
                    type = 6;
                    locus = _checkpointCreator.specOrient.locus;
                    break;
                case EditGM.EditCreatorTool.Warp:
                    type = 7;
                    locus = _warpCreator.specOrient.locus;
                    break;
                case EditGM.EditCreatorTool.Victory:
                    type = 8;
                    locus = _victoryCreator.specOrient.locus;
                    break;
                default:
                    return _lastFrameInfoPack;
            }
        }
        // Otherwise - In this case the editor is in edit mode
        else if (_isAnyItemSelected)
        {
            if (selectedItem.tileData.HasValue)
            {
                if (_isInstanceNull)
                {
                    // if instance is null, gather info from currently active tool
                    type = _tileCreator.tileType;
                    color = _tileCreator.tileColor;
                    spec = _tileCreator.tileSpecial;
                    rot = _tileCreator.tileOrient.rotation;
                    locus = _tileCreator.tileOrient.locus;
                    doorID = _tileCreator.tileDoorID;
                }
                else
                {
                    // if instance is non-null, gather info from object data
                    TileData td = selectedItem.tileData.Value;
                    type = (int)td.type;
                    color = (int)td.color;
                    spec = td.special;
                    rot = td.orient.rotation;
                    locus = td.orient.locus;
                    doorID = td.doorID;
                }
            }
            if (selectedItem.checkpointData.HasValue)
            {
                CheckpointData cd = selectedItem.checkpointData.Value;
                type = 6;
                if (_isInstanceNull)
                    // if instance is null, gather info from correct creator tool
                    locus = _checkpointCreator.specOrient.locus;
                else
                    // if instance is non-null, gather info from object data
                    locus = selectedItem.checkpointData.Value.locus;
            }
            if (selectedItem.warpData.HasValue)
            {
                WarpData wd = selectedItem.warpData.Value;
                type = 7;
                if (_isInstanceNull)
                {
                    // if instance is null, gather info from correct creator tool
                    rot = _warpCreator.specOrient.rotation;
                    locus = _warpCreator.specOrient.locus;
                }
                else
                {
                    // if instance is non-null, gather info from object data
                    locus = wd.locus;
                }
            }
            if (selectedItem.victoryData.HasValue)
            {
                VictoryData vd = selectedItem.victoryData.Value;
                type = 8;

                if (_isInstanceNull)
                    // if instance is null, gather info from correct creator tool
                    locus = _victoryCreator.specOrient.locus;
                else
                    // if instance is non-null, gather info from object data
                    locus = vd.locus;
            }
        }

        InfoPack infoPack = new InfoPack(type, color, spec, rot, locus, doorID);

        return infoPack;
    }

    // updates the display image
    private void updateUI(InfoPack infoPack)
    {
        EnsureReferences();

        if (_isAnyItemSelected && infoPack.type >= 0)
        {
            // set sprite source transform and aspect ratio for object image
            Transform t = _tileCreator.transform;

            // Tiles
            if (infoPack.type <= 5)
            {
                t = _tileCreator.transform.GetChild(infoPack.type);
                t = t.GetChild(infoPack.color).GetChild(0);
                _objectDisplayARF.aspectRatio = _aspectRatios[infoPack.type];
            }

            // Specials
            if (infoPack.type == 6)
            {
                t = _checkpointCreator.transform.GetChild(0);
                _objectDisplayARF.aspectRatio = 1f;
            }
            if (infoPack.type == 7)
            {
                t = _warpCreator.transform.GetChild(1);
                _objectDisplayARF.aspectRatio = 1f;
            }
            if (infoPack.type == 8)
            {
                t = _victoryCreator.transform.GetChild(0);
                _objectDisplayARF.aspectRatio = 1f;
            }

            // check for sprite, activate display as appropriate
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr)
            {
                _objectDisplay.sprite = sr.sprite;
                _objectDisplay.gameObject.SetActive(true);
            }
            else
                _objectDisplay.gameObject.SetActive(false);
        }
        else
            _objectDisplay.gameObject.SetActive(false);

        /* Object Panel Text Setting */
        // General approach - if we have a selected tile and it has the associated property - set the text in the object viewing panel
        // Combined Name (use standard for tile types (0-5) and special switch for 6, 7, 8 (checkpoint, warp, and victory))
        if (_isAnyItemSelected)
        {
            if (infoPack.type >= 0 && infoPack.color >= 0 && infoPack.type < 6)
            {
                if (_combinedNameDisplay != null)
                {
                    SafeSetText(
                        ref _combinedNameDisplay,
                        transform,
                        "Combined Name",
                        _colorStrings[infoPack.color] + " " + _typeStrings[infoPack.type]
                    );
                }
            }
            else
            {
                // special names switcher
                if (_combinedNameDisplay != null)
                {
                    switch (infoPack.type)
                    {
                        case 6:
                            SafeSetText(
                                ref _combinedNameDisplay,
                                transform,
                                "Combined Name",
                                "Checkpoint"
                            );
                            break;
                        case 7:
                            SafeSetText(
                                ref _combinedNameDisplay,
                                transform,
                                "Combined Name",
                                "Warp"
                            );
                            break;
                        case 8:
                            SafeSetText(
                                ref _combinedNameDisplay,
                                transform,
                                "Combined Name",
                                "Victory"
                            );
                            break;
                        default:
                            SafeSetText(
                                ref _combinedNameDisplay,
                                transform,
                                "Combined Name",
                                "Unknown Special Type"
                            );
                            break;
                    }
                }
            }
        }
        else if (_combinedNameDisplay != null)
        {
            SafeSetText(ref _combinedNameDisplay, transform, "Combined Name", "None Selected");
        }

        // Type (Not displayed at the moment)
        if (_typeDisplay != null && _isAnyItemSelected && infoPack.type >= 0 && infoPack.type < 6)
            SafeSetText(ref _typeDisplay, _standardAttributesPanel, "Type Display", _typeStrings[infoPack.type]);
        else if (_typeDisplay != null)
            SafeSetText(ref _typeDisplay, _standardAttributesPanel, "Type Display", "[N/A]");

        // Color (Not displayed at the moment)
        if (_colorDisplay != null && _isAnyItemSelected && infoPack.color >= 0)
            SafeSetText(ref _colorDisplay, _standardAttributesPanel, "Color Display", _colorStrings[infoPack.color]);
        else if (_colorDisplay != null)
            SafeSetText(ref _colorDisplay, _standardAttributesPanel, "Color Display", "[N/A]");

        // Rotation
        if (_rotationDisplay != null && _isAnyItemSelected && infoPack.rot >= 0)
            SafeSetText(ref _rotationDisplay, _standardAttributesPanel, "Rotation Display", infoPack.rot.ToString());
        else if (_rotationDisplay != null)
            SafeSetText(ref _rotationDisplay, _standardAttributesPanel, "Rotation Display", "[N/A]");

        // Locus
        if (_locusDisplay != null && _isAnyItemSelected)
            SafeSetText(ref _locusDisplay, _standardAttributesPanel, "Position Display", infoPack.locus.PrettyPrint());
        else if (_locusDisplay != null)
            SafeSetText(ref _locusDisplay, _standardAttributesPanel, "Position Display", "[N/A]");

        // set special dropdown values, activate if appropriate
        // Green - Special Text
        if (_specialLabel != null && _specialDisplay != null && infoPack.color == 3)
        {
            SafeSetText(ref _specialLabel, _specialPanel, "Special Label", "Key Id:");
            SafeSetText(ref _specialDisplay, _specialPanel, "Special Display", infoPack.spec.ToString());
        }

        // Orange - Special Text
        if (_specialLabel != null && _specialDisplay != null && infoPack.color == 4)
        {
            SafeSetText(ref _specialLabel, _specialPanel, "Special Label", "Gravity Direction:");
            SafeSetText(ref _specialDisplay, _specialPanel, "Special Display", infoPack.spec.ToString());
        }

        // Door ID
        // Only show door id tiles
        transform.GetChild(2).gameObject.SetActive(infoPack.type >= 0 && infoPack.type < 6);
        if (_doorIDDisplay != null)
            SafeSetText(ref _doorIDDisplay, _doorIdPanel, "DoorID Display", infoPack.doorID.ToString());

        // Only show special attributes for green and orange tiles
        transform.GetChild(3).gameObject.SetActive(infoPack.color == 3 || infoPack.color == 4);
    }

    private static TMP_Text FindText(Transform root, string childName)
    {
        if (root == null)
            return null;

        Transform child = root.Find(childName);
        if (child != null)
            return child.GetComponent<TMP_Text>();

        for (int i = 0; i < root.childCount; i++)
        {
            TMP_Text nested = FindText(root.GetChild(i), childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void EnsureReferences()
    {
        if (_standardAttributesPanel == null && transform.childCount > 1)
            _standardAttributesPanel = transform.GetChild(1);
        if (_doorIdPanel == null && transform.childCount > 2)
            _doorIdPanel = transform.GetChild(2);
        if (_specialPanel == null && transform.childCount > 3)
            _specialPanel = transform.GetChild(3);

        if (_combinedNameDisplay == null)
            _combinedNameDisplay = FindText(transform, "Combined Name");
        if (_typeDisplay == null)
            _typeDisplay = FindText(_standardAttributesPanel, "Type Display");
        if (_colorDisplay == null)
            _colorDisplay = FindText(_standardAttributesPanel, "Color Display");
        if (_rotationDisplay == null)
            _rotationDisplay = FindText(_standardAttributesPanel, "Rotation Display");
        if (_locusDisplay == null)
            _locusDisplay = FindText(_standardAttributesPanel, "Position Display");
        if (_specialLabel == null)
            _specialLabel = FindText(_specialPanel, "Special Label");
        if (_specialDisplay == null)
            _specialDisplay = FindText(_specialPanel, "Special Display");
        if (_doorIDLabel == null)
            _doorIDLabel = FindText(_doorIdPanel, "DoorID Label");
        if (_doorIDDisplay == null)
            _doorIDDisplay = FindText(_doorIdPanel, "DoorID Display");
    }

    private static void SafeSetText(ref TMP_Text textComponent, Transform searchRoot, string childName, string value)
    {
        if (textComponent == null)
            textComponent = FindText(searchRoot, childName);
        if (textComponent == null)
            return;

        try
        {
            textComponent.text = value;
        }
        catch (System.NullReferenceException)
        {
            textComponent = FindText(searchRoot, childName);
            if (textComponent != null)
                textComponent.text = value;
        }
    }

    private void BindRuntimeListeners()
    {
        if (_increaseDoorIdButton != null)
            _increaseDoorIdButton.onClick.AddListener(_editGM.IncrementDoorId);
        if (_decreaseDoorIdButton != null)
            _decreaseDoorIdButton.onClick.AddListener(_editGM.DecrementDoorId);
        if (_increaseSpecialButton != null)
            _increaseSpecialButton.onClick.AddListener(_editGM.IncrementKeyId);
        if (_decreaseSpecialButton != null)
            _decreaseSpecialButton.onClick.AddListener(_editGM.DecrementKeyId);

        if (_doorIdInputField != null)
        {
            _doorIdInputField.onEndEdit.AddListener(_tileCreator.SetDoorID);
            _doorIdInputField.onEndEdit.AddListener(_editGM.SetSelectedItemDoorID);
        }

        if (_specialInputField != null)
        {
            _specialInputField.onEndEdit.AddListener(_editGM.SetSelectedItemSpecial);
            _specialInputField.onEndEdit.AddListener(_tileCreator.SetSpecial);
        }
    }

    private static void CachePanelButtons(
        Transform panel,
        out Button decreaseButton,
        out Button increaseButton
    )
    {
        decreaseButton = null;
        increaseButton = null;

        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            switch (button.gameObject.name)
            {
                case "Decrease Button":
                    decreaseButton = button;
                    break;
                case "Increase Button":
                    increaseButton = button;
                    break;
            }
        }
    }
}
