using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Handles rebinding a single action binding and keeps the UI updated.
/// Pair it with a Button + label for each gameplay action you want to expose.
/// </summary>
public class InputRebindEntry : MonoBehaviour
{
	[SerializeField] private InputActionReference actionReference;
	[SerializeField] private int bindingIndex;
	[Tooltip("Optional binding id (GUID) to target a specific binding instead of an index.")]
	[SerializeField] private string bindingId;

	[SerializeField] private TMP_Text actionNameLabel;
	[SerializeField] private TMP_Text bindingDisplayLabel;
	[SerializeField] private Button rebindButton;
	[SerializeField] private Button resetButton;
	[SerializeField] private string displayNameOverride;
	[SerializeField] private string waitingForInputText = "Press a key...";

	private PlayerInputProvider inputProvider;
	private InputActionRebindingExtensions.RebindingOperation activeOperation;
	private InputAction currentAction;
	private bool currentActionWasEnabled;

	public void SetProvider(PlayerInputProvider provider)
	{
		inputProvider = provider;
	}

	private void Awake()
	{
		EnsureProvider();
		BindButtons();
		UpdateLabels();
	}

	private void OnEnable()
	{
		if (inputProvider != null)
			inputProvider.OnBindingsChanged += HandleBindingsChanged;

		UpdateLabels();
	}

	private void OnDisable()
	{
		if (inputProvider != null)
			inputProvider.OnBindingsChanged -= HandleBindingsChanged;

		CancelRebindOperation();
	}

	public void Refresh()
	{
		UpdateLabels();
	}

	private void EnsureProvider()
	{
		if (inputProvider == null)
			inputProvider = PlayerInputProvider.GetOrCreate();
	}

	private void BindButtons()
	{
		if (rebindButton != null)
		{
			rebindButton.onClick.RemoveListener(HandleRebindClicked);
			rebindButton.onClick.AddListener(HandleRebindClicked);
		}

		if (resetButton != null)
		{
			resetButton.onClick.RemoveListener(HandleResetClicked);
			resetButton.onClick.AddListener(HandleResetClicked);
		}
	}

	private void HandleRebindClicked()
	{
		StartRebind();
	}

	private void HandleResetClicked()
	{
		var action = ResolveAction();
		if (action == null)
			return;

		int index = ResolveBindingIndex(action);
		if (index < 0)
			return;

		action.RemoveBindingOverride(index);
		inputProvider?.SaveBindings();
		UpdateLabels();
	}

	private void StartRebind()
	{
		if (activeOperation != null)
			return;

		var action = ResolveAction();
		if (action == null)
			return;

		int index = ResolveBindingIndex(action);
		if (index < 0 || action.bindings[index].isComposite)
			return;

		CancelRebindOperation();
		SetWaitingState(true);

		currentAction = action;
		currentActionWasEnabled = action.enabled;
		action.Disable();
		activeOperation = action.PerformInteractiveRebinding(index)
			.WithCancelingThrough("<Keyboard>/escape")
			.OnMatchWaitForAnother(0.05f)
			.OnComplete(_ => CompleteRebind())
			.OnCancel(_ => CancelRebind());
		activeOperation.Start();
	}

	private void CompleteRebind()
	{
		if (currentAction != null && currentActionWasEnabled)
			currentAction.Enable();

		CleanupOperation();

		inputProvider?.SaveBindings();
		SetWaitingState(false);
		UpdateLabels();
	}

	private void CancelRebind()
	{
		if (currentAction != null && currentActionWasEnabled)
			currentAction.Enable();

		CleanupOperation();

		SetWaitingState(false);
		UpdateLabels();
	}

	private void CancelRebindOperation()
	{
		if (activeOperation == null)
			return;

		activeOperation.Cancel();
		CleanupOperation(true);
		SetWaitingState(false);
	}

	private void CleanupOperation(bool restoreState = false)
	{
		if (restoreState && currentAction != null && currentActionWasEnabled)
			currentAction.Enable();

		activeOperation?.Dispose();
		activeOperation = null;
		currentAction = null;
		currentActionWasEnabled = false;
	}

	private void UpdateLabels()
	{
		var action = ResolveAction();
		if (actionNameLabel != null)
			actionNameLabel.text = GetActionDisplayName(action);

		if (bindingDisplayLabel != null)
			bindingDisplayLabel.text = GetBindingDisplayName(action);
	}

	private string GetActionDisplayName(InputAction action)
	{
		if (!string.IsNullOrWhiteSpace(displayNameOverride))
			return displayNameOverride;

		if (action != null)
			return action.name;

		return "Unassigned";
	}

	private string GetBindingDisplayName(InputAction action)
	{
		if (action == null)
			return "-";

		int index = ResolveBindingIndex(action);
		if (index < 0)
			return "-";

		return action.GetBindingDisplayString(index, InputBinding.DisplayStringOptions.DontIncludeInteractions);
	}

	private InputAction ResolveAction()
	{
		if (actionReference == null)
			return null;

		EnsureProvider();
		return inputProvider.FindAction(actionReference);
	}

	private int ResolveBindingIndex(InputAction action)
	{
		if (action == null)
			return -1;

		if (!string.IsNullOrWhiteSpace(bindingId))
		{
			for (int i = 0; i < action.bindings.Count; i++)
			{
				if (string.Equals(action.bindings[i].id.ToString(), bindingId, StringComparison.OrdinalIgnoreCase))
					return i;
			}
		}

		if (bindingIndex >= 0 && bindingIndex < action.bindings.Count)
			return bindingIndex;

		return -1;
	}

	private void SetWaitingState(bool waiting)
	{
		if (waiting && bindingDisplayLabel != null && !string.IsNullOrWhiteSpace(waitingForInputText))
			bindingDisplayLabel.text = waitingForInputText;

		if (rebindButton != null)
			rebindButton.interactable = !waiting;

		if (resetButton != null)
			resetButton.interactable = !waiting;
	}

	private void HandleBindingsChanged(InputActionAsset _)
	{
		UpdateLabels();
	}
}
