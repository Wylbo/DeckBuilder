using System.Collections;
using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    [SerializeField] private MaterialPropertyRef[] DissolveProperties;

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
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float dissolveAmount = Mathf.Clamp01(elapsed / dissolveDuration);

            foreach (var property in DissolveProperties)
            {
                property.SetValue(dissolveCurve.Evaluate(dissolveAmount));
            }

            yield return null;
        }
    }
}