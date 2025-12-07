using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    private struct RendererProperty
    {
        public Renderer Renderer;
        public int MaterialIndex;
    }

    [SerializeField] private Transform dissolveParent;
    [SerializeField] private string dissolvePropertyName = "_Dissolve";

    [SerializeField] private float dissolveDuration = 1f;
    [SerializeField] private float waitBeforeDissolve = 0.5f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private readonly List<RendererProperty> dissolveTargets = new List<RendererProperty>();
    private MaterialPropertyBlock propertyBlock;
    private int dissolvePropertyId = Shader.PropertyToID("_Dissolve");
    private bool dissolveTargetsBuilt;

    private void Awake()
    {
        EnsureDissolveTargets();
    }

    public void ResetVisualState()
    {
        StopAllCoroutines();
        SetDissolveAmount(0f);
    }

    public void Dissolve(System.Action onComplete = null)
    {
        StopAllCoroutines();
        StartCoroutine(DissolveCoroutine(onComplete));
    }

    private IEnumerator DissolveCoroutine(System.Action onComplete)
    {
        yield return new WaitForSeconds(waitBeforeDissolve);
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float dissolveAmount = Mathf.Clamp01(elapsed / dissolveDuration);
            SetDissolveAmount(dissolveAmount);
            yield return null;
        }

        onComplete?.Invoke();
    }

    private void SetDissolveAmount(float dissolveAmount)
    {
        EnsureDissolveTargets();

        float evaluatedValue = dissolveCurve.Evaluate(dissolveAmount);
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        foreach (var target in dissolveTargets)
        {
            propertyBlock.Clear();
            target.Renderer.GetPropertyBlock(propertyBlock, target.MaterialIndex);
            propertyBlock.SetFloat(dissolvePropertyId, evaluatedValue);
            target.Renderer.SetPropertyBlock(propertyBlock, target.MaterialIndex);
        }
    }

    private void EnsureDissolveTargets()
    {
        if (dissolveTargetsBuilt)
        {
            return;
        }

        dissolveTargetsBuilt = true;
        dissolveTargets.Clear();

        var propertyName = string.IsNullOrEmpty(dissolvePropertyName) ? "_Dissolve" : dissolvePropertyName;
        dissolvePropertyId = Shader.PropertyToID(propertyName);

        var root = dissolveParent ? dissolveParent : transform;
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null) continue;

            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat != null && mat.HasProperty(dissolvePropertyId))
                {
                    dissolveTargets.Add(new RendererProperty
                    {
                        Renderer = renderer,
                        MaterialIndex = i
                    });
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        dissolveTargetsBuilt = false;
        if (Application.isPlaying)
        {
            EnsureDissolveTargets();
        }
    }
#endif
}
