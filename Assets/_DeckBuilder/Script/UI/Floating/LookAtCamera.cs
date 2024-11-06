using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCam;
    private void OnEnable()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        transform.forward = mainCam.transform.forward;
    }
}
