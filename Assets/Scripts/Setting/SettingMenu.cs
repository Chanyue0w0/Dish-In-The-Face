using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// ===== Localization =====
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SettingMenu : MonoBehaviour
{
	[Header("Panels (Root)")]
	[SerializeField] private GameObject settingPanel;
	[SerializeField] private GameObject normalPanel;
	[SerializeField] private GameObject staffPanel;
	[SerializeField] private GameObject keyPanel;

	[Header("-------- UI Reference --------")]
	[Header("Volume UI")]
	[SerializeField] private Slider masterSlider;
	[SerializeField] private TextMeshProUGUI masterVolumeText;

	[SerializeField] private Slider musicSlider;
	[SerializeField] private TextMeshProUGUI musicVolumeText;

	[SerializeField] private Slider sfxSlider;
	[SerializeField] private TextMeshProUGUI sfxVolumeText;

	[Header("Main Buttons Text (Display current settings)")]
	[SerializeField] private TextMeshProUGUI displayModeMainText;
	[SerializeField] private TextMeshProUGUI resolutionMainText;
	[SerializeField] private TextMeshProUGUI refreshRateMainText;
	[SerializeField] private TextMeshProUGUI languageMainText;
	[SerializeField] private TextMeshProUGUI vsyncMainText;       // x / v
	[SerializeField] private TextMeshProUGUI vibrationMainText;   // x / v

	[Header("Option Panels (Click to show detailed settings)")]
	[SerializeField] private GameObject displayModePanel;   // Contains displayModeButtons
	[SerializeField] private GameObject resolutionPanel;    // Contains resolutionButtons
	[SerializeField] private GameObject refreshRatePanel;   // Contains refreshRateButtons
	[SerializeField] private GameObject languagePanel;      // Contains languageButtons

	[Header("Buttons (Store individual setting buttons)")]
	[SerializeField] private Image[] displayModeButtons;
	[SerializeField] private Image[] resolutionButtons;
	[SerializeField] private Image[] refreshRateButtons;
	[SerializeField] private Image[] languageButtons;

	private List<GameObject> rootPanels = new List<GameObject>();
	private List<GameObject> optionPanels = new List<GameObject>();

	private bool isOpened = false;

	// ===== Localization configuration (cache available Locale) =====
	private AsyncOperationHandle m_LocInitOp;
	private Locale _localeZhTw;   // Chinese (Traditional) (zh-TW)
	private Locale _localeEnUs;   // English (United States) (en-US)

	private void Start()
	{
		// Root panels management (General / Staff / Controls)
		rootPanels.Add(normalPanel);
		rootPanels.Add(staffPanel);
		rootPanels.Add(keyPanel);

		// Option panels management (Display Mode / Resolution / Refresh Rate / Language)
		optionPanels.Add(displayModePanel);
		optionPanels.Add(resolutionPanel);
		optionPanels.Add(refreshRatePanel);
		optionPanels.Add(languagePanel);

		// Set default to first panel
		OnClickPanel(normalPanel);

		// Initialize volume settings (load from saved preferences)
		InitVolumeSetting();

		settingPanel.SetActive(false);
		isOpened = false;

		// ===== Localization initialization and configuration =====
		m_LocInitOp = LocalizationSettings.SelectedLocaleAsync;
		if (m_LocInitOp.IsDone)
			CacheLocales();
		else
			m_LocInitOp.Completed += _ => CacheLocales();
	}

	private void CacheLocales()
	{
		var locales = LocalizationSettings.AvailableLocales.Locales;
		foreach (var loc in locales)
		{
			// Use Identifier.Code first, fallback to LocaleName
			if (loc.Identifier.Code == "zh-TW" || loc.LocaleName == "Chinese (Traditional) (zh-TW)")
				_localeZhTw = loc;

			if (loc.Identifier.Code == "en-US" || loc.LocaleName == "English (United States) (en-US)")
				_localeEnUs = loc;
		}

		// Warn if not found (to avoid issues with missing Locale later)
		if (_localeZhTw == null)
			Debug.LogWarning("[SettingMenu] Locale zh-TW not found in Available Locales.");
		if (_localeEnUs == null)
			Debug.LogWarning("[SettingMenu] Locale en-US not found in Available Locales.");
	}

	private void Update()
	{
		// When Setting panel is opened, sync display text and settings
		if (settingPanel.activeInHierarchy && !isOpened)
		{
			isOpened = true;
			SyncMainTextsAndTogglesFromPrefs();
			OnClickCloseAllOptionPanels();
		}
	}

	#region Initialize: Volume settings (from saved preferences)
	private void InitVolumeSetting()
	{
		// Master
		float master = PlayerPrefsManager.GetMasterVolume();
		masterSlider.value = master;
		OnValueChangedMasterVolume();

		// Music
		float music = PlayerPrefsManager.GetMusicVolume();
		musicSlider.value = music;
		OnValueChangedMusicVolume();

		// SFX
		float sfx = PlayerPrefsManager.GetSFXVolume();
		sfxSlider.value = sfx;
		OnValueChangedSFXVolume();
	}
	#endregion

	#region Sync UI text and settings from PlayerPrefs when enabled
	private void SyncMainTextsAndTogglesFromPrefs()
	{
		// Display Mode
		string displayMode = PlayerPrefsManager.GetDisplayMode();
		if (displayModeMainText != null) displayModeMainText.text = displayMode;

		// Resolution
		string resolution = PlayerPrefsManager.GetResolution();
		if (resolutionMainText != null) resolutionMainText.text = resolution;

		// Refresh Rate
		string refreshRate = PlayerPrefsManager.GetRefreshRate();
		if (refreshRateMainText != null) refreshRateMainText.text = refreshRate;

		// Language (display text, actual switching happens in OnClickSelectLanguage)
		string language = PlayerPrefsManager.GetLanguage();
		if (languageMainText != null) languageMainText.text = language;

		// VSync (x / v)
		bool vsync = PlayerPrefsManager.GetVSync();
		if (vsyncMainText != null) vsyncMainText.text = vsync ? "v" : "x";
		QualitySettings.vSyncCount = vsync ? 1 : 0;

		// ����_�ʡ]x / v�^
		bool vibration = PlayerPrefsManager.GetControllerVibration();
		if (vibrationMainText != null) vibrationMainText.text = vibration ? "v" : "x";
		if (RumbleManager.Instance != null) RumbleManager.Instance.SetEnableRumble(vibration);
	}
	#endregion

	#region Exit
	public void OnClickExit()
	{
		settingPanel.SetActive(false);
	}
	#endregion

	#region Volume Settings (Sliders and Buttons)
	public void OnValueChangedMasterVolume()
	{
		masterVolumeText.text = ((int)(masterSlider.value * 100f)).ToString();
		PlayerPrefsManager.SetMasterVolume(masterSlider.value);
		AudioManager.Instance.masterVolume = masterSlider.value;
	}
	public void OnClickMasterVolume(bool increase)
	{
		masterSlider.value += increase ? 0.01f : -0.01f;
		masterSlider.value = Mathf.Clamp(masterSlider.value, 0f, 1f);
		OnValueChangedMasterVolume();
	}

	public void OnValueChangedMusicVolume()
	{
		musicVolumeText.text = ((int)(musicSlider.value * 100f)).ToString();
		PlayerPrefsManager.SetMusicVolume(musicSlider.value);
		AudioManager.Instance.musicVolume = musicSlider.value;
	}
	public void OnClickMusicVolume(bool increase)
	{
		musicSlider.value += increase ? 0.01f : -0.01f;
		musicSlider.value = Mathf.Clamp(musicSlider.value, 0f, 1f);
		OnValueChangedMusicVolume();
	}

	public void OnValueChangedSFXVolume()
	{
		sfxVolumeText.text = ((int)(sfxSlider.value * 100f)).ToString();
		PlayerPrefsManager.SetSFXVolume(sfxSlider.value);
		AudioManager.Instance.SFXVolume = sfxSlider.value;
	}
	public void OnClickSFXVolume(bool increase)
	{
		sfxSlider.value += increase ? 0.01f : -0.01f;
		sfxSlider.value = Mathf.Clamp(sfxSlider.value, 0f, 1f);
		OnValueChangedSFXVolume();
	}
	#endregion

	#region ��ܼҦ� / �ѪR�� / ��s�v / �y���G�D���s -> ���}���
	public void OnClickOpenOptionPanel(GameObject panel)
	{
		OpenOnlyThisOptionPanel(panel);
	}
	#endregion

	#region ��歱�O���G��ڮM�� + ��s�D���s��r + �������
	// -------- Display Mode --------
	public void OnClickSelectDisplayMode(string mode)
	{
		PlayerPrefsManager.SetDisplayMode(mode);

		if (displayModeMainText != null) displayModeMainText.text = mode;

		if (mode == "Borderless Windowed") Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
		else if (mode == "Windowed Mode") Screen.fullScreenMode = FullScreenMode.Windowed;
		else if (mode == "Fullscreen") Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;

		displayModePanel.SetActive(false);
	}

	// -------- Resolution --------
	public void OnClickSelectResolution(string resolution)
	{
		PlayerPrefsManager.SetResolution(resolution);

		if (resolutionMainText != null) resolutionMainText.text = resolution;

		string[] parts = resolution.Split('x');
		if (parts.Length == 2 &&
			int.TryParse(parts[0], out int w) &&
			int.TryParse(parts[1], out int h))
		{
			Screen.SetResolution(w, h, Screen.fullScreenMode);
		}

		resolutionPanel.SetActive(false);
	}

	// -------- Refresh Rate --------
	public void OnClickSelectRefreshRate(string rate)
	{
		PlayerPrefsManager.SetRefreshRate(rate);
		if (refreshRateMainText != null) refreshRateMainText.text = rate;
		// �u���M�Χ�s�v�q�`�ݭn�f�t�ѪR�שΦb�Ұʮɳ]�w�A�o�̶ȫO�s�P���
		refreshRatePanel.SetActive(false);
	}

	// -------- Language --------
	/// <summary>
	/// lang �ӦۧA�� UI�A�Ҧp "English" / "����" / "en-US" / "zh-TW"
	/// �Ȥ䴩��� Locale�Gzh-TW �P en-US
	/// </summary>
	public void OnClickSelectLanguage(string lang)
	{
		// ��ܬ����]�����A�쥻���ߺD�^
		PlayerPrefsManager.SetLanguage(lang);
		if (languageMainText != null) languageMainText.text = lang;

		// �|����l�ƴN�������]�קK NRE�^
		if (!m_LocInitOp.IsDone)
		{
			Debug.LogWarning("[SettingMenu] Localization not initialized yet.");
			if (languagePanel != null) languagePanel.SetActive(false);
			return;
		}

		// �M�w�ؼ� Locale
		Locale target = null;

		// ���\�h�ؿ�J�]�r���ΥN�X�^
		if (lang == "zh-TW" || lang.Contains("����") || lang.Contains("Chinese"))
			target = _localeZhTw;
		else if (lang == "en-US" || lang.Contains("English"))
			target = _localeEnUs;

		// �䤣��N�����]�����ܡ^
		if (target == null)
		{
			Debug.LogWarning($"[SettingMenu] Target locale not resolved for '{lang}'.");
		}
		else if (LocalizationSettings.SelectedLocale != target)
		{
			LocalizationSettings.Instance.SetSelectedLocale(target);
		}

		// �������O
		if (languagePanel != null) languagePanel.SetActive(false);
	}
	#endregion

	#region VSync / �_�ʡG��@�����]x <-> v�^
	public void OnClickToggleVSync()
	{
		bool current = PlayerPrefsManager.GetVSync();
		bool next = !current;

		PlayerPrefsManager.SetVSync(next);
		QualitySettings.vSyncCount = next ? 1 : 0;

		if (vsyncMainText != null) vsyncMainText.text = next ? "v" : "x";
	}

	public void OnClickToggleVibration()
	{
		bool current = PlayerPrefsManager.GetControllerVibration();
		bool next = !current;

		PlayerPrefsManager.SetControllerVibration(next);
		if (RumbleManager.Instance != null) RumbleManager.Instance.SetEnableRumble(next);

		if (vibrationMainText != null) vibrationMainText.text = next ? "v" : "x";
	}
	#endregion

	#region �@�P�G����/���O�}���]�L�C���ܴ��^
	public void OnClickPanel(GameObject panel)
	{
		foreach (GameObject p in rootPanels)
			p.SetActive(panel == p);
	}

	private void OpenOnlyThisOptionPanel(GameObject target)
	{
		foreach (var p in optionPanels)
			p.SetActive(p == target);
	}

	public void OnClickCloseAllOptionPanels()
	{
		foreach (var p in optionPanels)
			if (p != null) p.SetActive(false);
	}
	#endregion
}
