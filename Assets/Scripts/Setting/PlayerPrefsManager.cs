using UnityEngine;

public static class PlayerPrefsManager
{
	// ---------------- 預設值（可調整） ----------------
	public static float DefaultMasterVolume = 1f;
	public static float DefaultMusicVolume = 1f;
	public static float DefaultSFXVolume = 1f;
	public static string DefaultDisplayMode = "Windowed Mode"; // Borderless Windowed, Windowed Mode, Fullscreen
	public static string DefaultResolution = "1920x1080";       // 1280x720, 1600x900, 1920x1080
	public static bool DefaultVSync = true;
	public static string DefaultRefreshRate = "60";             // 60, 120, 144, Unlimited
	public static bool DefaultControllerVibration = true;
	public static string DefaultLanguage = "English";
	// ---------------------------------------------------

	// --- Keys ---
	private const string MASTER_VOLUME_KEY = "MasterVolume";
	private const string MUSIC_VOLUME_KEY = "MusicVolume";
	private const string SFX_VOLUME_KEY = "SFXVolume";
	private const string DISPLAY_MODE_KEY = "DisplayMode";
	private const string RESOLUTION_KEY = "Resolution";
	private const string VSYNC_KEY = "VSync";
	private const string REFRESH_RATE_KEY = "RefreshRate";
	private const string CONTROLLER_VIBRATION_KEY = "ControllerVibration";
	private const string LANGUAGE_KEY = "Language";

	// --- Master Volume ---
	public static void SetMasterVolume(float value)
	{
		PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, value);
		Save();
	}
	public static float GetMasterVolume() => PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DefaultMasterVolume);

	// --- Music Volume ---
	public static void SetMusicVolume(float value)
	{
		PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, value);
		Save();
	}
	public static float GetMusicVolume() => PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DefaultMusicVolume);

	// --- SFX Volume ---
	public static void SetSFXVolume(float value)
	{
		PlayerPrefs.SetFloat(SFX_VOLUME_KEY, value);
		Save();
	}
	public static float GetSFXVolume() => PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DefaultSFXVolume);

	// --- Display Mode ---
	public static void SetDisplayMode(string value)
	{
		PlayerPrefs.SetString(DISPLAY_MODE_KEY, value);
		Save();
	}
	public static string GetDisplayMode() => PlayerPrefs.GetString(DISPLAY_MODE_KEY, DefaultDisplayMode);

	// --- Resolution ---
	public static void SetResolution(string value)
	{
		PlayerPrefs.SetString(RESOLUTION_KEY, value);
		Save();
	}
	public static string GetResolution() => PlayerPrefs.GetString(RESOLUTION_KEY, DefaultResolution);

	// --- V-Sync ---
	public static void SetVSync(bool value)
	{
		PlayerPrefs.SetInt(VSYNC_KEY, value ? 1 : 0);
		Save();
	}
	public static bool GetVSync() => PlayerPrefs.GetInt(VSYNC_KEY, DefaultVSync ? 1 : 0) == 1;

	// --- Refresh Rate ---
	public static void SetRefreshRate(string value)
	{
		PlayerPrefs.SetString(REFRESH_RATE_KEY, value);
		Save();
	}
	public static string GetRefreshRate() => PlayerPrefs.GetString(REFRESH_RATE_KEY, DefaultRefreshRate);

	// --- Controller Vibration ---
	public static void SetControllerVibration(bool value)
	{
		PlayerPrefs.SetInt(CONTROLLER_VIBRATION_KEY, value ? 1 : 0);
		Save();
	}
	public static bool GetControllerVibration() => PlayerPrefs.GetInt(CONTROLLER_VIBRATION_KEY, DefaultControllerVibration ? 1 : 0) == 1;

	// --- Language ---
	public static void SetLanguage(string value)
	{
		PlayerPrefs.SetString(LANGUAGE_KEY, value);
		Save();
	}
	public static string GetLanguage() => PlayerPrefs.GetString(LANGUAGE_KEY, DefaultLanguage);

	// --- Save / Clear ---
	public static void Save() => PlayerPrefs.Save();
	public static void ClearAllSettings()
	{
		PlayerPrefs.DeleteAll();
		Save();
	}

	// --- Reset 所有設定為預設值 ---
	public static void ResetToDefault()
	{
		SetMasterVolume(DefaultMasterVolume);
		SetMusicVolume(DefaultMusicVolume);
		SetSFXVolume(DefaultSFXVolume);
		SetDisplayMode(DefaultDisplayMode);
		SetResolution(DefaultResolution);
		SetVSync(DefaultVSync);
		SetRefreshRate(DefaultRefreshRate);
		SetControllerVibration(DefaultControllerVibration);
		SetLanguage(DefaultLanguage);
		// 每個 Set 內已經 Save 過，因此這邊不再重複 Save
	}
}
