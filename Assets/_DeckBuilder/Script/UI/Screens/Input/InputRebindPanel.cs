using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Coordinates a group of <see cref="InputRebindEntry"/> widgets and offers a single reset-all button.
/// Drop this on the Input tab inside Options and hook up the entries in the inspector.
/// </summary>
public class InputRebindPanel : MonoBehaviour
{
	[SerializeField] private InputRebindEntry[] entries;
	[SerializeField] private Button resetAllButton;
	[SerializeField] private bool applySavedBindingsOnEnable = true;

	PlayerInputProvider InputProvider => PlayerInputProvider.Instance;

	private void Awake()
	{
		BindResetButton();
		AssignProviderToEntries();
	}

	private void OnEnable()
	{
		if (InputProvider != null)
			InputProvider.OnBindingsChanged += HandleBindingsChanged;

		if (applySavedBindingsOnEnable)
			InputProvider?.ApplySavedBindings();

		RefreshEntries();
	}

	private void OnDisable()
	{
		if (InputProvider != null)
			InputProvider.OnBindingsChanged -= HandleBindingsChanged;
	}

	private void HandleBindingsChanged(InputActionAsset _)
	{
		RefreshEntries();
	}

	private void RefreshEntries()
	{
		if (entries == null)
			return;

		foreach (var entry in entries)
		{
			entry?.SetProvider(InputProvider);
			entry?.Refresh();
		}
	}

	private void HandleResetAllClicked()
	{
		if (InputProvider == null)
			return;

		InputProvider.ResetBindingsToDefaults();
		InputProvider.ApplySavedBindings();
		RefreshEntries();
	}

	private void BindResetButton()
	{
		if (resetAllButton == null)
			return;

		resetAllButton.onClick.RemoveListener(HandleResetAllClicked);
		resetAllButton.onClick.AddListener(HandleResetAllClicked);
	}

	private void AssignProviderToEntries()
	{
		if (entries == null)
			return;

		foreach (var entry in entries)
		{
			if (entry == null)
				continue;

			entry.SetProvider(InputProvider);
		}
	}
}
