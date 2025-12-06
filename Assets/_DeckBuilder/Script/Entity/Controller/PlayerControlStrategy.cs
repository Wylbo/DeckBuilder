using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Orchestrates player input by composing small handlers (move, abilities, UI toggles, mode switching).
/// Keeps lifecycle wiring here so individual concerns stay testable and focused.
/// </summary>
[CreateAssetMenu(fileName = nameof(PlayerControlStrategy), menuName = FileName.Controller + nameof(PlayerControlStrategy), order = 0)]
public class PlayerControlStrategy : ControlStrategy
{
	[SerializeField] private LayerMask groundLayerMask;
	[SerializeField] private GameObject moveClickVFX;
	[SerializeField] private float groundRayDistance = 100f;

	private PlayerInputProvider inputProvider;
	private PlayerInputs playerInput;

	private MouseGroundRaycaster groundRaycaster;
	private ClickMoveHandler moveHandler;
	private AbilityInputHandler abilityHandler;
	private UiInputModeSwitcher uiModeSwitcher;
	private UiCancelHandler uiCancelHandler;
	[SerializeField] private MinimapUIToggle minimapToggle;
	private readonly List<UIToggleHandler> uiToggleHandlers = new List<UIToggleHandler>();

	public override void Initialize(Controller controller, Character character, IUIManager uiManager = null)
	{
		base.Initialize(controller, character, uiManager);

		inputProvider = PlayerInputProvider.GetOrCreate();
		playerInput = inputProvider.Inputs;
		inputProvider.ApplySavedBindings();

		groundRaycaster = new MouseGroundRaycaster(groundLayerMask, groundRayDistance);
		moveHandler = new ClickMoveHandler(playerInput.Gameplay.Move, controller, groundRaycaster, new PoolVfxSpawner(), moveClickVFX);
		abilityHandler = BuildAbilityHandler(controller);
		uiModeSwitcher = new UiInputModeSwitcher(playerInput, UiManager, HasAnyBlockingUIVisible, moveHandler.StopContinuousMove);
		uiCancelHandler = new UiCancelHandler(playerInput.UI.Cancel, UiManager);

		BuildUIToggles();

		moveHandler.Enable();
		abilityHandler.Enable();
		uiModeSwitcher.Enable();
		uiCancelHandler.Enable();
		EnableUIToggles();
		RegisterGameplayCallbacks();

		Debug.Log("Player controller strategy initialized");
	}

	public override void Disable()
	{
		if (playerInput == null)
			return;

		moveHandler?.Dispose();
		abilityHandler?.Dispose();
		uiModeSwitcher?.Dispose();
		uiCancelHandler?.Dispose();
		DisposeUIToggles();
		UnregisterGameplayCallbacks();

		playerInput.Gameplay.Disable();
		playerInput.UI.Disable();
	}

	// called each frame by the controller
	public override void Control(float deltaTime)
	{
		moveHandler?.Tick();
	}

	private AbilityInputHandler BuildAbilityHandler(Controller controller)
	{
		var handler = new AbilityInputHandler(controller, groundRaycaster);
		handler.AddAbility(playerInput.Gameplay.Ability1, 0);
		handler.AddAbility(playerInput.Gameplay.Ability2, 1);
		handler.AddAbility(playerInput.Gameplay.Ability3, 2);
		handler.AddAbility(playerInput.Gameplay.Ability4, 3);
		return handler;
	}

	private void BuildUIToggles()
	{
		uiToggleHandlers.Clear();
		if (UiManager == null)
			return;

		uiToggleHandlers.Add(CreateToggle<AbilityInventoryView>(playerInput.Gameplay.OpenInventory));
		uiToggleHandlers.Add(CreateToggle<MenuScreenView>(playerInput.Gameplay.OpenMenu));
		// Add more toggles here as new UI views become toggleable.
	}

	private UIToggleHandler CreateToggle<TView>(InputAction action) where TView : UIView
	{
		return new UIToggleHandler(
			action,
			() => UiManager != null && UiManager.IsVisible<TView>(),
			() => UiManager?.Show<TView>(),
			() => UiManager?.Hide<TView>());
	}

	private void EnableUIToggles()
	{
		for (int i = 0; i < uiToggleHandlers.Count; i++)
			uiToggleHandlers[i].Enable();
	}

	private void DisposeUIToggles()
	{
		for (int i = 0; i < uiToggleHandlers.Count; i++)
			uiToggleHandlers[i].Dispose();

		uiToggleHandlers.Clear();
	}

	private void RegisterGameplayCallbacks()
	{
		playerInput.Gameplay.Dodge.performed += Dodge_performed;
		playerInput.Gameplay.ToggleMinimap.performed += ToggleMinimap_performed;
	}

	private void UnregisterGameplayCallbacks()
	{
		playerInput.Gameplay.Dodge.performed -= Dodge_performed;
		playerInput.Gameplay.ToggleMinimap.performed -= ToggleMinimap_performed;
	}

	private void Dodge_performed(InputAction.CallbackContext context)
	{
		groundRaycaster.TryGetPoint(out Vector3 worldPos);
		controller.PerformDodge(worldPos);
	}

	private void ToggleMinimap_performed(InputAction.CallbackContext context)
	{
		var toggle = GetMinimapToggle();
		toggle?.Toggle();
	}

	private MinimapUIToggle GetMinimapToggle()
	{
		if (minimapToggle == null)
		{
			minimapToggle = FindFirstObjectByType<MinimapUIToggle>();
		}

		return minimapToggle;
	}

	private bool HasAnyBlockingUIVisible()
	{
		if (UiManager == null)
			return false;

		return UiManager.HasVisibleViewOnLayer(UILayer.Screen) || UiManager.HasVisibleViewOnLayer(UILayer.Popup);
	}
}
