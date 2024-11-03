using System;
using MG.Extend;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = nameof(PlayerControlStrategy), menuName = FileName.Controller + nameof(PlayerControlStrategy), order = 0)]
public class PlayerControlStrategy : ControlStrategy
{
	[SerializeField]
	private LayerMask groundLayerMask;
	[SerializeField]
	private GameObject moveClickVFX;

	private PlayerInputs playerInput;

	public override void Initialize(Controller controller)
	{
		base.Initialize(controller);

		playerInput = new PlayerInputs();

		playerInput.Gameplay.Enable();

		playerInput.Gameplay.Move.performed += Move_performed;
		playerInput.Gameplay.Ability1.performed += Ability1_performed;
		playerInput.Gameplay.Ability2.performed += Ability2_performed;
		playerInput.Gameplay.Ability3.performed += Ability3_performed;
		playerInput.Gameplay.Ability4.performed += Ability4_performed;

		playerInput.Gameplay.Ability1.canceled += Ability1_canceled;
		playerInput.Gameplay.Ability2.canceled += Ability2_canceled;
		playerInput.Gameplay.Ability3.canceled += Ability3_canceled;
		playerInput.Gameplay.Ability4.canceled += Ability4_canceled;
		Debug.Log("Player controller strategy initialized");
	}

	public override void Disable()
	{
		playerInput.Gameplay.Move.performed -= Move_performed;
		playerInput.Gameplay.Ability1.performed -= Ability1_performed;
		playerInput.Gameplay.Ability2.performed -= Ability2_performed;
		playerInput.Gameplay.Ability3.performed -= Ability3_performed;
		playerInput.Gameplay.Ability4.performed -= Ability4_performed;

		playerInput.Gameplay.Disable();
	}

	public override void Control()
	{

	}

	private void Move_performed(InputAction.CallbackContext ctx)
	{
		if (GetMousePositionInWorld(out Vector3 worldPos))
		{
			PoolManager.Provide(moveClickVFX, worldPos, Quaternion.identity, PoolManager.PoolType.VFX);
			controller.TryMove(worldPos);
		}
	}

	private bool GetMousePositionInWorld(out Vector3 worldPosition)
	{
		worldPosition = new Vector3();

		Vector3 viewportPosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
		Ray ray = Camera.main.ViewportPointToRay(viewportPosition);

		if (Physics.Raycast(ray, out RaycastHit info, 100, groundLayerMask))
		{
			worldPosition = info.point;

			DebugDrawer.DrawSphere(worldPosition, .3f, Color.cyan, 2f);
			return true;
		}

		return false;
	}

	private void Ability4_performed(InputAction.CallbackContext obj)
	{
		PerformAbility(3);
	}

	private void Ability3_performed(InputAction.CallbackContext obj)
	{
		PerformAbility(2);
	}

	private void Ability2_performed(InputAction.CallbackContext obj)
	{
		PerformAbility(1);
	}

	private void Ability1_performed(InputAction.CallbackContext obj)
	{
		PerformAbility(0);
	}

	private void Ability1_canceled(InputAction.CallbackContext obj)
	{
		EndHold(0);
	}

	private void Ability2_canceled(InputAction.CallbackContext obj)
	{
		EndHold(1);
	}

	private void Ability3_canceled(InputAction.CallbackContext obj)
	{
		EndHold(2);
	}

	private void Ability4_canceled(InputAction.CallbackContext obj)
	{
		EndHold(3);
	}

	private void PerformAbility(int index)
	{
		GetMousePositionInWorld(out Vector3 worldPos);
		controller.CastAbility(index, worldPos);
	}

	private void EndHold(int index)
	{
		GetMousePositionInWorld(out Vector3 worldPos);
		controller.EndHold(index, worldPos);
	}
}
