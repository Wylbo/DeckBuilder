using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityChanneled), menuName = FileName.Ability + nameof(AbilityChanneled))]
public class AbilityChanneled : Ability
{
	#region fields
	[Space]
	[SerializeField] protected bool followCursorDuringChanneling;
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
	[SerializeField] private LayerMask groundLayerMask = 0;
	#endregion

	#region runtime variables
	protected Coroutine channelRoutine = null;
	protected GameObject spawnedVFX = null;
	protected float channeledRatio = 0;
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

		spawnedVFX = PoolManager.Provide(channelingVFX, Caster.transform.position, Caster.transform.rotation, Caster.transform, PoolManager.PoolType.VFX);
		channelRoutine = Caster.StartCoroutine(UpdateChanneling(worldPos));
	}

	protected virtual IEnumerator UpdateChanneling(Vector3 worldPos)
	{
		float elapsed = 0;
		while (elapsed < channelDuration)
		{
			if (followCursorDuringChanneling)
			{
				LookAtCursorPosition();
			}
			// need to check elapsed > 0 because isHeld is not update at frame 0
			if (holdToChannel && !isHeld && elapsed > 0)
			{
				break;
			}

			if (movingInterupChanneling && movement.IsMoving)
			{
				break;
			}

			elapsed += Time.deltaTime;

			channeledRatio = Mathf.Clamp01(elapsed / channelDuration);

			Debug.Log($"[{nameof(AbilityChanneled)}] channeling {channeledRatio * 100:F1}% | isHeld: {isHeld}");

			yield return null;
		}

		EndCast(worldPos, elapsed >= channelDuration);
	}

	public override void EndCast(Vector3 worldPos, bool isSucessful = true)
	{
		base.EndCast(worldPos, isSucessful);

		PoolManager.Release(spawnedVFX);
		movement.EnableMovement();
		movement.ResetSpeed();
	}


	protected void LookAtCursorPosition()
	{

		Vector3 viewportPosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
		Ray ray = Camera.main.ViewportPointToRay(viewportPosition);

		Vector3 worldPosition = Caster.transform.forward;
		if (Physics.Raycast(ray, out RaycastHit info, 100, groundLayerMask))
		{
			worldPosition = info.point;

		}

		Vector3 castDirection = worldPosition - Caster.transform.position;
		castDirection.y = 0;
		Caster.transform.LookAt(Caster.transform.position + castDirection);
	}
	#endregion
}