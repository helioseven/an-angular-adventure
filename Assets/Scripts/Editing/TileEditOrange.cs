using circleXsquares;
using UnityEngine;

public class TileEditOrange : MonoBehaviour
{
    // private consts
    private const int ARROW_CHILD_INDEX = 2;

    // private references
    private EditGM _editGM;

    void Start()
    {
        _editGM = EditGM.instance;
    }

    /* Public Functions */

    // turns the arrow icon according to passed direction
    public void SetGravityDirection(GravityDirection inDirection)
    {
        Transform arrowIcon = transform.GetChild(ARROW_CHILD_INDEX);

        int intRot = ((int)inDirection + 1) % 4;
        if (intRot % 2 == 1)
            intRot += 2;
        Vector3 rotation = Vector3.forward * (intRot * 90);

        arrowIcon.rotation = Quaternion.Euler(rotation);
    }
}
