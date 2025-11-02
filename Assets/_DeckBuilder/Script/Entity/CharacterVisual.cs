using System.Collections;
using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private float dissolveDuration = 1f;
    [SerializeField] private float waitBeforeDissolve = 0.5f;
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public void Dissolve()
    {
        StartCoroutine(DissolveCoroutine());
    }

    IEnumerator DissolveCoroutine()
    {
        yield return new WaitForSeconds(waitBeforeDissolve);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float dissolveAmount = Mathf.Clamp01(elapsed / dissolveDuration);
            block.SetFloat("_Dissolve", dissolveCurve.Evaluate(dissolveAmount));

            foreach (var renderer in renderers)
            {
                renderer.SetPropertyBlock(block);
            }

            yield return null;
        }
    }
}