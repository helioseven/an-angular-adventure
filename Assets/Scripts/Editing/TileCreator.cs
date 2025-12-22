using System;
using System.Collections;
using circleXsquares;
using UnityEngine;

/* The TileCreator is how new tiles are added to the level in the editor.
 * It behaves differently depending on which editor mode is active.
 *
 * At its root, the TileCreator works by having every possible
 * shape/color combination of tile instantiated in the same location,
 * as children of the parent object (which owns this script),
 * and turning on/off the renderers for those children.
 */

public class TileCreator : MonoBehaviour
{
    // public read-accessibility state variables
    public int tileType { get; private set; }
    public int tileColor { get; private set; }
    public int tileSpecial { get; private set; }
    public HexOrient tileOrient { get; private set; }
    public int tileDoorID { get; private set; }

    // private consts
    private const int ARROW_OR_KEY_CHILD_INDEX = 2;
    private const int DOOR_CHILD_INDEX = 1;
    private const int SPRITE_CHILD_INDEX = 0;

    // private references
    private SpriteRenderer[,] _tileRenderers;

    // private variables
    private SnapCursor _anchorRef;
    private EditGM _gmRef;

    void Start()
    {
        _gmRef = EditGM.instance;
        _anchorRef = _gmRef.anchorIcon;
        tileType = 0;
        tileColor = 0;
        tileSpecial = 0;
        tileDoorID = 0;
        tileOrient = new HexOrient(new HexLocus(), 0, 0);

        int nTypes = Constants.NUM_SHAPES;
        int nColors = Constants.NUM_COLORS;
        _tileRenderers = new SpriteRenderer[nTypes, nColors];

        for (int i = 0; i < nTypes; i++)
        {
            for (int j = 0; j < nColors; j++)
            {
                Transform t = transform.GetChild(i).GetChild(j);

                // gets the sprite renderer for each of the tile types and colors
                _tileRenderers[i, j] = t.GetChild(SPRITE_CHILD_INDEX)
                    .GetComponent<SpriteRenderer>();
                _tileRenderers[i, j].enabled = false;

                // also turn off all icons to begin with
                t.GetChild(DOOR_CHILD_INDEX).gameObject.SetActive(false);
                if (j == 3 || j == 4)
                    t.GetChild(ARROW_OR_KEY_CHILD_INDEX).gameObject.SetActive(false);
            }
        }

        // turns all renderers off except the active tile
        _tileRenderers[tileType, tileColor].enabled = true;
    }

    void Update()
    {
        // when active, the TileCreator will follow the focus
        HexLocus f = _anchorRef.focus;
        int r = tileOrient.rotation;
        int l = _gmRef.activeLayer;
        tileOrient = new HexOrient(f, r, l);

        Quaternion q;
        transform.position = tileOrient.ToUnitySpace(out q);
        transform.rotation = q;
    }

    /* Public Functions */

    // disables and enables renderers based on passed type
    public void SelectType(int inType)
    {
        _tileRenderers[tileType, tileColor].enabled = false;
        tileType = inType % _tileRenderers.GetLength(0);
        _tileRenderers[tileType, tileColor].enabled = true;
    }

    // disables and enables renderers based on color
    public void CycleColor(bool clockwise)
    {
        int count = _tileRenderers.GetLength(1);
        _tileRenderers[tileType, tileColor].enabled = false;
        // we add count so that modulus doesn't choke on a negative number
        int newColor = count + (clockwise ? tileColor + 1 : tileColor - 1);
        tileColor = newColor % count;
        _tileRenderers[tileType, tileColor].enabled = true;
    }

    // set door id
    public void SetDoorID(string inId)
    {
        int doorID = 0;

        if (int.TryParse(inId, out int parsedId))
        {
            doorID = parsedId;
        }
        else
        {
            Debug.LogWarning($"Could not parse door ID from input: \"{inId}\"");
        }

        tileDoorID = doorID;
    }

    // sets tile's special value if valid color is in use
    public void SetSpecial(string inSpecial)
    {
        int special = int.Parse(inSpecial);
        if (tileColor == 3)
        {
            // green (door unlock)
            tileSpecial = special;
        }
        if (tileColor == 4)
        {
            // orange (gravity)
            tileSpecial = (special + 4) % 4;
        }
    }

    // turns the transform in 30 degree increments
    public void SetRotation(int inRotation)
    {
        tileOrient = new HexOrient(tileOrient.locus, inRotation, tileOrient.layer);
        Update();
    }

    // translates and rotates the transform according to given orientation
    public void SetOrientation(HexOrient inOrient)
    {
        tileOrient = inOrient;
        Update();
    }

    // sets type, color, and rotation by passed struct
    public void SetProperties(TileData inData)
    {
        _tileRenderers[tileType, tileColor].enabled = false;
        tileType = inData.type;
        tileColor = inData.color;
        tileSpecial = inData.special;
        tileDoorID = inData.doorID;
        _tileRenderers[tileType, tileColor].enabled = true;
        SetRotation(inData.orient.rotation);
    }

    // returns a TileData representation of the genesisTile's current state
    public TileData GetTileData()
    {
        return new TileData(tileType, tileColor, tileSpecial, tileOrient, tileDoorID);
    }

    // returns a new tile copied from the tile in active use
    public GameObject GetActiveCopy()
    {
        GameObject go = _tileRenderers[tileType, tileColor].transform.parent.gameObject;
        go = Instantiate(go, go.transform.position, go.transform.rotation) as GameObject;

        return go;
    }

    // returns an instantiated copy of a specified tile
    public GameObject NewTile(TileData inData)
    {
        // rather than change TileCreator itself,
        // use it's GameObjects to instantiate a copy as specified
        GameObject go = _tileRenderers[inData.type, inData.color].transform.parent.gameObject;
        Quaternion r = Quaternion.Euler(0, 0, 30 * inData.orient.rotation);
        Vector3 p = inData.orient.locus.ToUnitySpace();
        p.z = _gmRef.GetLayerDepth();

        go = Instantiate(go, p, r) as GameObject;
        // make sure the renderer is on before handing the GameObject off
        go.GetComponentInChildren<SpriteRenderer>().enabled = true;

        return go;
    }
}
