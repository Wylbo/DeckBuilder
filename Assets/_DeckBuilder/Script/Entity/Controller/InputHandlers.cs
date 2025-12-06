using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MG.Extend;

/// <summary>
/// Converts mouse position to ground hits via raycast. Keeps Camera/main usage contained.
/// </summary>
internal interface IMouseGroundRaycaster
{
	bool TryGetPoint(out Vector3 worldPosition);
}

/// <summary>
/// Default ground raycaster that uses a layer mask and optional camera to find hit points.
/// </summary>
internal sealed class MouseGroundRaycaster : IMouseGroundRaycaster
{
	private readonly LayerMask groundLayerMask;
	private readonly float maxDistance;
	private readonly Camera targetCamera;

	public MouseGroundRaycaster(LayerMask groundLayerMask, float maxDistance = 100f, Camera targetCamera = null)
	{
		this.groundLayerMask = groundLayerMask;
		this.maxDistance = maxDistance;
		this.targetCamera = targetCamera;
	}

	public bool TryGetPoint(out Vector3 worldPosition)
	{
		worldPosition = Vector3.zero;

		var camera = targetCamera != null ? targetCamera : Camera.main;
		if (camera == null)
			return false;

		Vector3 viewportPosition = camera.ScreenToViewportPoint(Input.mousePosition);
		Ray ray = camera.ViewportPointToRay(viewportPosition);

		if (Physics.Raycast(ray, out RaycastHit info, maxDistance, groundLayerMask))
		{
			worldPosition = info.point;
			DebugDrawer.DrawSphere(worldPosition, .3f, Color.cyan, 2f);
			return true;
		}

		return false;
	}
}

/// <summary>
/// Abstraction for spawning VFX so the controller does not depend on PoolManager directly.
/// </summary>
internal interface IVfxSpawner
{
	void Spawn(GameObject prefab, Vector3 position);
}

internal sealed class PoolVfxSpawner : IVfxSpawner
{
	public void Spawn(GameObject prefab, Vector3 position)
	{
		if (prefab == null)
			return;

		PoolManager.Provide(prefab, position, Quaternion.identity, null, PoolManager.PoolType.VFX);
	}
}

/// <summary>
/// Handles click/hold to move, including VFX and continuous movement ticking.
/// </summary>
internal sealed class ClickMoveHandler : IDisposable
{
	private readonly InputAction moveAction;
	private readonly Controller controller;
	private readonly IMouseGroundRaycaster raycaster;
	private readonly IVfxSpawner vfxSpawner;
	private readonly GameObject moveClickVfx;

	private bool isMoving;

	public ClickMoveHandler(InputAction moveAction, Controller controller, IMouseGroundRaycaster raycaster, IVfxSpawner vfxSpawner, GameObject moveClickVfx)
	{
		this.moveAction = moveAction;
		this.controller = controller;
		this.raycaster = raycaster;
		this.vfxSpawner = vfxSpawner;
		this.moveClickVfx = moveClickVfx;
	}

	public void Enable()
	{
		moveAction.performed += HandleMovePerformed;
		moveAction.canceled += HandleMoveCanceled;
	}

	public void Disable()
	{
		moveAction.performed -= HandleMovePerformed;
		moveAction.canceled -= HandleMoveCanceled;
	}

	public void Tick()
	{
		if (!isMoving)
			return;

		if (raycaster.TryGetPoint(out Vector3 worldPos))
		{
			controller.TryMove(worldPos);
		}
	}

	public void StopContinuousMove()
	{
		isMoving = false;
	}

	private void HandleMovePerformed(InputAction.CallbackContext ctx)
	{
		isMoving = true;

		if (raycaster.TryGetPoint(out Vector3 worldPos))
		{
			vfxSpawner?.Spawn(moveClickVfx, worldPos);
			controller.TryMove(worldPos);
		}
	}

	private void HandleMoveCanceled(InputAction.CallbackContext ctx)
	{
		isMoving = false;
	}

	public void Dispose()
	{
		Disable();
	}
}

/// <summary>
/// Maps input actions to ability indices and forwards performed/canceled to the controller.
/// </summary>
internal sealed class AbilityInputHandler : IDisposable
{
	private readonly IMouseGroundRaycaster raycaster;
	private readonly Controller controller;
	private readonly List<(InputAction action, int index)> abilityActions = new List<(InputAction action, int index)>();

	public AbilityInputHandler(Controller controller, IMouseGroundRaycaster raycaster)
	{
		this.controller = controller;
		this.raycaster = raycaster;
	}

	public void AddAbility(InputAction action, int index)
	{
		abilityActions.Add((action, index));
	}

	public void Enable()
	{
		foreach (var entry in abilityActions)
		{
			entry.action.performed += HandleAbilityPerformed;
			entry.action.canceled += HandleAbilityCanceled;
		}
	}

	public void Disable()
	{
		foreach (var entry in abilityActions)
		{
			entry.action.performed -= HandleAbilityPerformed;
			entry.action.canceled -= HandleAbilityCanceled;
		}
	}

	private void HandleAbilityPerformed(InputAction.CallbackContext context)
	{
		if (TryGetAbilityIndex(context.action, out int index))
		{
			raycaster.TryGetPoint(out Vector3 worldPos);
			controller.CastAbility(index, worldPos);
		}
	}

	private void HandleAbilityCanceled(InputAction.CallbackContext context)
	{
		if (TryGetAbilityIndex(context.action, out int index))
		{
			raycaster.TryGetPoint(out Vector3 worldPos);
			controller.EndHold(index, worldPos);
		}
	}

	private bool TryGetAbilityIndex(InputAction action, out int index)
	{
		for (int i = 0; i < abilityActions.Count; i++)
		{
			if (abilityActions[i].action == action)
			{
				index = abilityActions[i].index;
				return true;
			}
		}

		index = -1;
		return false;
	}

	public void Dispose()
	{
		Disable();
		abilityActions.Clear();
	}
}

/// <summary>
/// Switches between gameplay and UI action maps based on blocking UI visibility.
/// </summary>
internal sealed class UiInputModeSwitcher : IDisposable
{
	private readonly PlayerInputs playerInputs;
	private readonly IUIManager uiManager;
	private readonly Func<bool> hasBlockingUIVisible;
	private readonly Action onSwitchToUi;

	public UiInputModeSwitcher(PlayerInputs playerInputs, IUIManager uiManager, Func<bool> hasBlockingUIVisible, Action onSwitchToUi = null)
	{
		this.playerInputs = playerInputs;
		this.uiManager = uiManager;
		this.hasBlockingUIVisible = hasBlockingUIVisible;
		this.onSwitchToUi = onSwitchToUi;
	}

	public void Enable()
	{
		if (uiManager != null)
		{
			uiManager.AfterShow += HandleViewShown;
			uiManager.AfterHide += HandleViewHidden;
		}

		SwitchToGameplay();
	}

	public void Disable()
	{
		if (uiManager != null)
		{
			uiManager.AfterShow -= HandleViewShown;
			uiManager.AfterHide -= HandleViewHidden;
		}
	}

	private void HandleViewShown(UIView view)
	{
		if (IsBlockingView(view))
			SwitchToUi();
	}

	private void HandleViewHidden(UIView view)
	{
		if (IsBlockingView(view) && (hasBlockingUIVisible == null || !hasBlockingUIVisible()))
			SwitchToGameplay();
	}

	private static bool IsBlockingView(UIView view)
	{
		return view != null && (view.Layer == UILayer.Screen || view.Layer == UILayer.Popup);
	}

	private void SwitchToUi()
	{
		onSwitchToUi?.Invoke();
		playerInputs.Gameplay.Disable();
		playerInputs.UI.Enable();
	}

	public void SwitchToGameplay()
	{
		playerInputs.UI.Disable();
		playerInputs.Gameplay.Enable();
	}

	public void Dispose()
	{
		Disable();
	}
}

/// <summary>
/// Generic toggle that shows/hides a UI view when an input action is performed.
/// </summary>
internal sealed class UIToggleHandler : IDisposable
{
	private readonly InputAction inputAction;
	private readonly Func<bool> isVisible;
	private readonly Action show;
	private readonly Action hide;

	public UIToggleHandler(InputAction inputAction, Func<bool> isVisible, Action show, Action hide)
	{
		this.inputAction = inputAction;
		this.isVisible = isVisible;
		this.show = show;
		this.hide = hide;
	}

	public void Enable()
	{
		inputAction.performed += HandlePerformed;
	}

	public void Disable()
	{
		inputAction.performed -= HandlePerformed;
	}

	private void HandlePerformed(InputAction.CallbackContext context)
	{
		if (isVisible())
			hide?.Invoke();
		else
			show?.Invoke();
	}

	public void Dispose()
	{
		Disable();
	}
}

/// <summary>
/// Handles UI cancel action and hides the current view.
/// </summary>
internal sealed class UiCancelHandler : IDisposable
{
	private readonly InputAction cancelAction;
	private readonly IUIManager uiManager;

	public UiCancelHandler(InputAction cancelAction, IUIManager uiManager)
	{
		this.cancelAction = cancelAction;
		this.uiManager = uiManager;
	}

	public void Enable()
	{
		cancelAction.performed += HandleCancelPerformed;
	}

	public void Disable()
	{
		cancelAction.performed -= HandleCancelPerformed;
	}

	private void HandleCancelPerformed(InputAction.CallbackContext context)
	{
		uiManager?.HideCurrentView();
	}

	public void Dispose()
	{
		Disable();
	}
}
