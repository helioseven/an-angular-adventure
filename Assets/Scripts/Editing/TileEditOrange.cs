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
    public void SetGravityDirection(int inDirection)
    {
        inDirection %= 4;

        Transform arrowIcon = transform.GetChild(ARROW_CHILD_INDEX);

        int intRot = (inDirection + 1) % 4;
        if (intRot % 2 == 1)
            intRot += 2;
        Vector3 rotation = Vector3.forward * (intRot * 90);

        arrowIcon.localRotation = Quaternion.Euler(rotation - transform.rotation.eulerAngles);
    }
}
