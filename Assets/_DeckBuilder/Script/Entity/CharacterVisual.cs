using System.Collections;
using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
	[SerializeField] private MaterialPropertyRef[] DissolveProperties;

	[SerializeField] private float dissolveDuration = 1f;
	[SerializeField] private float waitBeforeDissolve = 0.5f;
	[SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.Linear(0, 0, 1, 1);

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

	IEnumerator DissolveCoroutine(System.Action onComplete)
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
                float evaluatedValue = dissolveCurve.Evaluate(dissolveAmount);
                foreach (var property in DissolveProperties)
                {
                        property.SetValue(evaluatedValue);
                }
        }
}
