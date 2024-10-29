using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityChanneled), menuName = FileName.Ability + nameof(AbilityChanneled))]
public class AbilityChanneled : Ability
{
	[Space]
	[SerializeField]
	protected float channelDuration = 0f;
	[SerializeField, Tooltip("Can an move command can be inputed during the channeling")]
	protected bool canMoveDuringChanneling = false;
	[SerializeField]
	protected bool movingInterupChanneling = false;

	protected Coroutine channelRoutine = null;

	protected override void DoCast(Vector3 worldPos)
	{
		StartChanneling(worldPos);
	}

	protected virtual void StartChanneling(Vector3 worldPos)
	{
		if (channelRoutine != null)
		{
			Caster.StopCoroutine(channelRoutine);
			channelRoutine = null;
		}

		if (!canMoveDuringChanneling)
			movement.DisableMovement();

		channelRoutine = Caster.StartCoroutine(UpdateChanneling(worldPos));
	}

	protected virtual IEnumerator UpdateChanneling(Vector3 worldPos)
	{
		float elapsed = 0;
		while (elapsed < channelDuration)
		{
			if (movingInterupChanneling && movement.IsMoving)
			{
				break;
			}

			elapsed += Time.deltaTime;

			Debug.Log($"[{nameof(AbilityChanneled)}] channeling {elapsed / channelDuration * 100:F1}%");

			yield return null;
		}

		EndChaneling(worldPos, elapsed >= channelDuration);
	}

	protected virtual void EndChaneling(Vector3 worldPos, bool fullyChanneled)
	{
		Debug.Log($"[{nameof(AbilityChanneled)}] full channeled: {fullyChanneled}");

		movement.EnableMovement();
		EndCast(worldPos);
	}

}
