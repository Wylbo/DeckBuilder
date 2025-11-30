using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Small helper for storing and restoring binding overrides from PlayerPrefs.
/// Keeps rebinding logic decoupled from UI and gameplay code.
/// </summary>
public static class InputBindingPersistence
{
	private const string PlayerPrefsKey = "PlayerInputBindingOverrides";

	public static void ApplySavedBindings(InputActionAsset asset)
	{
		if (asset == null || !PlayerPrefs.HasKey(PlayerPrefsKey))
			return;

		var json = PlayerPrefs.GetString(PlayerPrefsKey);
		if (string.IsNullOrWhiteSpace(json))
			return;

		asset.LoadBindingOverridesFromJson(json);
	}

	public static void SaveBindings(InputActionAsset asset)
	{
		if (asset == null)
			return;

		var json = asset.SaveBindingOverridesAsJson();
		PlayerPrefs.SetString(PlayerPrefsKey, json);
		PlayerPrefs.Save();
	}

	public static void ClearSavedBindings()
	{
		if (PlayerPrefs.HasKey(PlayerPrefsKey))
			PlayerPrefs.DeleteKey(PlayerPrefsKey);

		PlayerPrefs.Save();
	}
}
