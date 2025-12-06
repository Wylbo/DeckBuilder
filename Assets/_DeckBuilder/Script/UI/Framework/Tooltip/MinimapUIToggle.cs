using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUIToggle : MonoBehaviour
{
    [SerializeField] private RectTransform minimapRect;
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private Vector2 fullscreenSize = new Vector2(800f, 800f);
    [SerializeField] private Vector2 fullscreenPosition = Vector2.zero;
    [SerializeField] private Vector2 fullscreenAnchorMin = new Vector2(0f, 0f);
    [SerializeField] private Vector2 fullscreenAnchorMax = new Vector2(1f, 1f);
    [SerializeField] private float minimapCameraSize = 25f;
    [SerializeField] private float fullscreenCameraSize = 55f;
    [SerializeField] private RenderTexture miniMapRenderTexture;
    [SerializeField] private RenderTexture fullscreenRenderTexture;
    [SerializeField] private RawImage minimapOutput;

    private Vector2 _originalSize;
    private Vector2 _originalPosition;
    private Vector2 _originalAnchorMin;
    private Vector2 _originalAnchorMax;
    private Vector2 _originalPivot;
    private bool _isFullscreen;

    private void Awake()
    {
        if (minimapRect == null)
        {
            minimapRect = GetComponent<RectTransform>();
        }

        _originalSize = minimapRect.sizeDelta;
        _originalPosition = minimapRect.anchoredPosition;
        _originalAnchorMin = minimapRect.anchorMin;
        _originalAnchorMax = minimapRect.anchorMax;
        _originalPivot = minimapRect.pivot;

        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = minimapCameraSize;
            SetRenderTarget(miniMapRenderTexture);
        }
    }

    [Button]
    public void Toggle()
    {
        _isFullscreen = !_isFullscreen;

        if (_isFullscreen)
        {
            ApplyFullscreen();
        }
        else
        {
            ApplyMinimap();
        }
    }

    private void ApplyFullscreen()
    {
        minimapRect.anchorMin = fullscreenAnchorMin;
        minimapRect.anchorMax = fullscreenAnchorMax;
        minimapRect.pivot = new Vector2(0.5f, 0.5f);
        minimapRect.sizeDelta = fullscreenSize;
        minimapRect.anchoredPosition = fullscreenPosition;

        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = fullscreenCameraSize;
            SetRenderTarget(fullscreenRenderTexture != null ? fullscreenRenderTexture : miniMapRenderTexture);
        }
    }

    private void ApplyMinimap()
    {
        minimapRect.anchorMin = _originalAnchorMin;
        minimapRect.anchorMax = _originalAnchorMax;
        minimapRect.sizeDelta = _originalSize;
        minimapRect.anchoredPosition = _originalPosition;
        minimapRect.pivot = _originalPivot;

        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = minimapCameraSize;
            SetRenderTarget(miniMapRenderTexture);
        }
    }

    private void SetRenderTarget(RenderTexture target)
    {
        if (minimapCamera != null)
        {
            minimapCamera.targetTexture = target;
        }

        if (minimapOutput != null)
        {
            minimapOutput.texture = target;
        }
    }

}
