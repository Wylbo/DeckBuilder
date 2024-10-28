using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(ProjectileLauncher))]
public class AbilityCaster : MonoBehaviour
{
	[SerializeField]
	private Ability[] abilities = new Ability[4];
	[SerializeField]
	private ProjectileLauncher projectileLauncher = null;

	public ProjectileLauncher ProjectileLauncher => projectileLauncher;

	private void OnEnable()
	{
		InitializeAbilities();
	}

	private void OnDisable()
	{
		DisableAllAbilities();
	}

	private void Reset()
	{
		projectileLauncher = GetComponent<ProjectileLauncher>();
	}

	private void InitializeAbilities()
	{
		foreach (Ability ability in abilities)
		{
			ability.Initialize(this);
		}
	}

	private void DisableAllAbilities()
	{
		foreach (Ability ability in abilities)
		{
			ability.Disable();
		}
	}

	public void Cast(int index, Vector3 worldPos)
	{
		if (abilities[index].RotateCasterToCastDirection)
			LookAtCastDirection(worldPos);

		Cast(abilities[index]);
	}

	private void LookAtCastDirection(Vector3 worldPos)
	{
		Vector3 castDirection = worldPos - transform.position;
		castDirection.y = 0;
		Debug.DrawRay(transform.position, castDirection, Color.yellow, 1f);
		transform.LookAt(transform.position + castDirection);
	}

	private void Cast(Ability ability)
	{
		ability.Cast();
	}
}
