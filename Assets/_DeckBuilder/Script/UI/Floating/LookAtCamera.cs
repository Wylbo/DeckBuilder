using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main)
            transform.forward = Camera.main.transform.forward;
        else
            transform.forward = new Vector3(0, 0, 1);
    }
}
