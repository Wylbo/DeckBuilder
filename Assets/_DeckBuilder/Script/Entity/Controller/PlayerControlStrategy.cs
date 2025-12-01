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

	private PlayerInputProvider inputProvider;
	private PlayerInputs playerInput;

	private bool isMoving;
	private bool InventoryVisible => UiManager != null && UiManager.IsVisible<AbilityInventoryView>();

	public override void Initialize(Controller controller, Character character, IUIManager uiManager = null)
	{
		base.Initialize(controller, character, uiManager);

		inputProvider = PlayerInputProvider.GetOrCreate();
		playerInput = inputProvider.Inputs;
		inputProvider.ApplySavedBindings();

		RegisterGameplayCallbacks();
		RegisterUICallbacks();
		SubscribeToUIManager();

		SwitchToGameplayInput();

		Debug.Log("Player controller strategy initialized");
	}

	public override void Disable()
	{
		if (playerInput == null)
			return;

		UnsubscribeFromUIManager();
		UnregisterGameplayCallbacks();
		UnregisterUICallbacks();

		playerInput.Gameplay.Disable();
		playerInput.UI.Disable();
	}

	// called each frame by the controller
	public override void Control(float deltaTime)
	{
		if (isMoving && GetMousePositionInWorld(out Vector3 worldPos))
		{
			controller.TryMove(worldPos);
		}
	}

	private void Move_performed(InputAction.CallbackContext ctx)
	{
		isMoving = true;
		if (GetMousePositionInWorld(out Vector3 worldPos))
		{
			PoolManager.Provide(moveClickVFX, worldPos, Quaternion.identity, null, PoolManager.PoolType.VFX);
			controller.TryMove(worldPos);
		}
	}

	private void Move_canceled(InputAction.CallbackContext context)
	{
		isMoving = false;
	}

	private void ToggleInventory()
	{
		if (UiManager == null)
			return;

		if (InventoryVisible)
			UiManager.Hide<AbilityInventoryView>();
		else
			UiManager.Show<AbilityInventoryView>();
	}

	private void HandleViewShown(UIView view)
	{
		if (ShouldUseUIInput(view))
			SwitchToUIInput();
	}

	private void HandleViewHidden(UIView view)
	{
		if (ShouldUseUIInput(view) && !HasAnyBlockingUIVisible())
			SwitchToGameplayInput();
	}

	private void SwitchToUIInput()
	{
		isMoving = false;
		playerInput.Gameplay.Disable();
		playerInput.UI.Enable();
	}

	private void SwitchToGameplayInput()
	{
		playerInput.UI.Disable();
		playerInput.Gameplay.Enable();
	}

	private Vector3 GetMousePositionInWorld()
	{
		GetMousePositionInWorld(out Vector3 worldPosition);
		return worldPosition;
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

	private void Dodge_canceled(InputAction.CallbackContext context)
	{
	}

	private void Dodge_performed(InputAction.CallbackContext context)
	{
		controller.PerformDodge(GetMousePositionInWorld());
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

	private void OpenInventory_performed(InputAction.CallbackContext context)
	{
		ToggleInventory();
	}

	private void OpenMenuPerformed(InputAction.CallbackContext context)
	{
		UiManager?.Show<MenuScreenView>();
	}


	private void UICancel_performed(InputAction.CallbackContext context)
	{
		UiManager?.HideCurrentView();
	}

	private void RegisterGameplayCallbacks()
	{
		playerInput.Gameplay.Move.performed += Move_performed;
		playerInput.Gameplay.Dodge.performed += Dodge_performed;
		playerInput.Gameplay.Ability1.performed += Ability1_performed;
		playerInput.Gameplay.Ability2.performed += Ability2_performed;
		playerInput.Gameplay.Ability3.performed += Ability3_performed;
		playerInput.Gameplay.Ability4.performed += Ability4_performed;

		playerInput.Gameplay.Move.canceled += Move_canceled;
		playerInput.Gameplay.Ability1.canceled += Ability1_canceled;
		playerInput.Gameplay.Ability2.canceled += Ability2_canceled;
		playerInput.Gameplay.Ability3.canceled += Ability3_canceled;
		playerInput.Gameplay.Ability4.canceled += Ability4_canceled;
		playerInput.Gameplay.Dodge.canceled += Dodge_canceled;

		playerInput.Gameplay.OpenInventory.performed += OpenInventory_performed;
		playerInput.Gameplay.OpenMenu.performed += OpenMenuPerformed;
	}

	private void UnregisterGameplayCallbacks()
	{
		playerInput.Gameplay.Move.performed -= Move_performed;
		playerInput.Gameplay.Dodge.performed -= Dodge_performed;
		playerInput.Gameplay.Ability1.performed -= Ability1_performed;
		playerInput.Gameplay.Ability2.performed -= Ability2_performed;
		playerInput.Gameplay.Ability3.performed -= Ability3_performed;
		playerInput.Gameplay.Ability4.performed -= Ability4_performed;

		playerInput.Gameplay.Move.canceled -= Move_canceled;
		playerInput.Gameplay.Ability1.canceled -= Ability1_canceled;
		playerInput.Gameplay.Ability2.canceled -= Ability2_canceled;
		playerInput.Gameplay.Ability3.canceled -= Ability3_canceled;
		playerInput.Gameplay.Ability4.canceled -= Ability4_canceled;
		playerInput.Gameplay.Dodge.canceled -= Dodge_canceled;

		playerInput.Gameplay.OpenInventory.performed -= OpenInventory_performed;
		playerInput.Gameplay.OpenMenu.performed -= OpenMenuPerformed;
	}

	private void RegisterUICallbacks()
	{
		playerInput.UI.Cancel.performed += UICancel_performed;
	}

	private void UnregisterUICallbacks()
	{
		playerInput.UI.Cancel.performed -= UICancel_performed;
	}

	private void SubscribeToUIManager()
	{
		if (UiManager == null)
			return;

		UiManager.AfterShow += HandleViewShown;
		UiManager.AfterHide += HandleViewHidden;
	}

	private void UnsubscribeFromUIManager()
	{
		if (UiManager == null)
			return;

		UiManager.AfterShow -= HandleViewShown;
		UiManager.AfterHide -= HandleViewHidden;
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

	private bool ShouldUseUIInput(UIView view)
	{
		return view != null && (view.Layer == UILayer.Screen || view.Layer == UILayer.Popup);
	}

	private bool HasAnyBlockingUIVisible()
	{
		if (UiManager == null)
			return false;

		return UiManager.HasVisibleViewOnLayer(UILayer.Screen) || UiManager.HasVisibleViewOnLayer(UILayer.Popup);
	}
}
