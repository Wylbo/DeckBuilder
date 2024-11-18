using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCam;
    private void OnEnable()
    {
        mainCam = Camera.main;
    }

    void LateUpdate()
    {
        transform.forward = mainCam.transform.forward;
    }
}
