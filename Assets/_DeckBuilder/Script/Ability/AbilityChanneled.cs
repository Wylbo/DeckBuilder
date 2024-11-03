using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityChanneled), menuName = FileName.Ability + nameof(AbilityChanneled))]
public class AbilityChanneled : Ability
{
	#region fields
	[Space]
	[SerializeField]
	protected bool holdToChannel = false;
	[SerializeField]
	protected float channelDuration = 0f;
	[SerializeField, Tooltip("Can an move command can be inputed during the channeling")]
	protected bool canMoveDuringChanneling = false;
	[SerializeField]
	protected bool movingInterupChanneling = false;
	[SerializeField]
	protected GameObject channelingVFX = null;
	#endregion

	#region runtime variables
	protected Coroutine channelRoutine = null;
	protected GameObject spawnedVFX = null;
	#endregion

	#region AbilityChanneled
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

		spawnedVFX = PoolManager.Provide(channelingVFX, Caster.transform.position, Caster.transform.rotation, PoolManager.PoolType.VFX);
		channelRoutine = Caster.StartCoroutine(UpdateChanneling(worldPos));
	}

	protected virtual IEnumerator UpdateChanneling(Vector3 worldPos)
	{
		float elapsed = 0;
		while (elapsed < channelDuration)
		{
			if (holdToChannel && !isHeld && elapsed > 0)
			{
				break;
			}

			if (movingInterupChanneling && movement.IsMoving)
			{
				break;
			}

			elapsed += Time.deltaTime;

			Debug.Log($"[{nameof(AbilityChanneled)}] channeling {elapsed / channelDuration * 100:F1}% | isHeld: {isHeld}");

			yield return null;
		}

		EndCast(worldPos, elapsed >= channelDuration);
	}

	public override void EndCast(Vector3 worldPos, bool isSucessful = true)
	{
		base.EndCast(worldPos, isSucessful);
		Debug.Log($"[{nameof(AbilityChanneled)}] full channeled: {isSucessful}");

		PoolManager.Release(spawnedVFX);
		movement.EnableMovement();
	}
	#endregion
}