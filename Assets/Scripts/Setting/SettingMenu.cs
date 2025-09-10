using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Sirenix.OdinInspector;

// ===== Localization =====
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

[Searchable]
public class SettingMenu : MonoBehaviour
{
	[Title("主要面板")]
	[BoxGroup("面板系統")]
	[LabelText("設定面板")]
	[SerializeField] private GameObject settingPanel;
	
	[BoxGroup("面板系統/分頁面板")]
	[LabelText("一般設定")]
	[SerializeField] private GameObject normalPanel;
	
	[BoxGroup("面板系統/分頁面板")]
	[LabelText("製作團隊")]
	[SerializeField] private GameObject staffPanel;
	
	[BoxGroup("面板系統/分頁面板")]
	[LabelText("按鍵設定")]
	[SerializeField] private GameObject keyPanel;

	[Title("音量控制")]
	[FoldoutGroup("音量設定", expanded: true)]
	[HorizontalGroup("音量設定/")]
	[LabelText("主音量滑桿")]
	[SerializeField] private Slider masterSlider;
	
	[HorizontalGroup("音量設定/主音量")]
	[LabelText("主音量數值顯示")]
	[SerializeField] private TextMeshProUGUI masterVolumeText;

	[HorizontalGroup("音量設定/音樂")]
	[LabelText("音樂滑桿")]
	[SerializeField] private Slider musicSlider;
	
	[HorizontalGroup("音量設定/音樂")]
	[LabelText("音樂數值顯示")]
	[SerializeField] private TextMeshProUGUI musicVolumeText;

	[HorizontalGroup("音量設定/音效")]
	[LabelText("音效滑桿")]
	[SerializeField] private Slider sfxSlider;
	
	[HorizontalGroup("音量設定/音效")]
	[LabelText("音效數值顯示")]
	[SerializeField] private TextMeshProUGUI sfxVolumeText;

	[Title("顯示設定")]
	[TabGroup("設定選項", "顯示")]
	[BoxGroup("設定選項/顯示/主要文字")]
	[LabelText("顯示模式")]
	[SerializeField] private TextMeshProUGUI displayModeMainText;
	
	[BoxGroup("設定選項/顯示/主要文字")]
	[LabelText("解析度")]
	[SerializeField] private TextMeshProUGUI resolutionMainText;
	
	[BoxGroup("設定選項/顯示/主要文字")]
	[LabelText("更新率")]
	[SerializeField] private TextMeshProUGUI refreshRateMainText;
	
	[BoxGroup("設定選項/顯示/主要文字")]
	[LabelText("語言")]
	[SerializeField] private TextMeshProUGUI languageMainText;
	
	[BoxGroup("設定選項/顯示/主要文字")]
	[LabelText("垂直同步")]
	[InfoBox("顯示 x 或 v")]
	[SerializeField] private TextMeshProUGUI vsyncMainText;
	
	[BoxGroup("設定選項/顯示/主要文字")]
	[LabelText("震動")]
	[InfoBox("顯示 x 或 v")]
	[SerializeField] private TextMeshProUGUI vibrationMainText;
	
	[TabGroup("設定選項", "選項面板")]
	[BoxGroup("設定選項/選項面板/面板")]
	[LabelText("顯示模式面板")]
	[SerializeField] private GameObject displayModePanel;
	
	[BoxGroup("設定選項/選項面板/面板")]
	[LabelText("解析度面板")]
	[SerializeField] private GameObject resolutionPanel;
	
	[BoxGroup("設定選項/選項面板/面板")]
	[LabelText("更新率面板")]
	[SerializeField] private GameObject refreshRatePanel;
	
	[BoxGroup("設定選項/選項面板/面板")]
	[LabelText("語言面板")]
	[SerializeField] private GameObject languagePanel;

	[TabGroup("設定選項", "按鈕陣列")]
	[BoxGroup("設定選項/按鈕陣列/按鈕")]
	[LabelText("顯示模式按鈕")]
	[SerializeField] private Image[] displayModeButtons;
	
	[BoxGroup("設定選項/按鈕陣列/按鈕")]
	[LabelText("解析度按鈕")]
	[SerializeField] private Image[] resolutionButtons;
	
	[BoxGroup("設定選項/按鈕陣列/按鈕")]
	[LabelText("更新率按鈕")]
	[SerializeField] private Image[] refreshRateButtons;
	
	[BoxGroup("設定選項/按鈕陣列/按鈕")]
	[LabelText("語言按鈕")]
	[SerializeField] private Image[] languageButtons;

	[Title("運行時資料", "僅供除錯使用")]
	[FoldoutGroup("內部變數", expanded: false)]
	[ShowInInspector, ReadOnly]
	[LabelText("根面板列表")]
	private List<GameObject> rootPanels = new List<GameObject>();
	
	[FoldoutGroup("內部變數")]
	[ShowInInspector, ReadOnly]
	[LabelText("選項面板列表")]
	private List<GameObject> optionPanels = new List<GameObject>();

	[FoldoutGroup("內部變數")]
	[ShowInInspector, ReadOnly]
	[LabelText("面板已開啟")]
	private bool isOpened = false;

	[Title("本地化設定")]
	[FoldoutGroup("本地化", expanded: false)]
	[ShowInInspector, ReadOnly]
	[LabelText("本地化初始化")]
	private AsyncOperationHandle m_LocInitOp;
	
	[FoldoutGroup("本地化")]
	[ShowInInspector, ReadOnly]
	[LabelText("繁體中文")]
	private Locale _localeZhTw;   // Chinese (Traditional) (zh-TW)
	
	[FoldoutGroup("本地化")]
	[ShowInInspector, ReadOnly]
	[LabelText("美式英文")]
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
