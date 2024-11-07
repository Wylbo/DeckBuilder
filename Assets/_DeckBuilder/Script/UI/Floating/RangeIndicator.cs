using UnityEngine;

public class RangeIndicator : MonoBehaviour
{
    public void SetScale(float scale)
    {
        Vector3 newScale = new Vector3(scale, 1, scale);

        SetScale(newScale);
    }

    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;
    }
}
