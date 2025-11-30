using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Centralises access to <see cref="PlayerInputs"/> so gameplay and UI share the same instance and binding overrides.
/// Automatically applies saved overrides and exposes callbacks when bindings change.
/// </summary>
public class PlayerInputProvider : MonoBehaviour
{
	public static PlayerInputProvider Instance { get; private set; }

	[SerializeField] private bool dontDestroyOnLoad = true;

	private PlayerInputs playerInputs;

	public PlayerInputs Inputs
	{
		get
		{
			EnsureInputs();
			return playerInputs;
		}
	}

	public event Action<InputActionAsset> OnBindingsChanged;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		if (dontDestroyOnLoad)
			DontDestroyOnLoad(gameObject);

		EnsureInputs();
		ApplySavedBindings();
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	public static PlayerInputProvider GetOrCreate()
	{
		if (Instance != null)
			return Instance;

		var go = new GameObject(nameof(PlayerInputProvider));
		return go.AddComponent<PlayerInputProvider>();
	}

	public void ApplySavedBindings()
	{
		if (playerInputs == null)
			return;

		InputBindingPersistence.ApplySavedBindings(playerInputs.asset);
		NotifyBindingsChanged();
	}

	public void SaveBindings()
	{
		if (playerInputs == null)
			return;

		InputBindingPersistence.SaveBindings(playerInputs.asset);
		NotifyBindingsChanged();
	}

	public void ResetBindingsToDefaults()
	{
		if (playerInputs == null)
			return;

		playerInputs.asset.RemoveAllBindingOverrides();
		InputBindingPersistence.ClearSavedBindings();
		NotifyBindingsChanged();
	}

	public InputAction FindAction(InputActionReference reference, bool throwIfNotFound = false)
	{
		if (reference == null)
			return null;

		EnsureInputs();
		var actionId = reference.action != null ? reference.action.id.ToString() : reference.name;
		return playerInputs.asset.FindAction(actionId, throwIfNotFound);
	}

	private void EnsureInputs()
	{
		if (playerInputs != null)
			return;

		playerInputs = new PlayerInputs();
	}

	private void NotifyBindingsChanged()
	{
		OnBindingsChanged?.Invoke(playerInputs?.asset);
	}
}
