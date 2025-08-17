using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

	[Header("Main Buttons Text (顯示當前設定值)")]
	[SerializeField] private TextMeshProUGUI displayModeMainText;
	[SerializeField] private TextMeshProUGUI resolutionMainText;
	[SerializeField] private TextMeshProUGUI refreshRateMainText;
	[SerializeField] private TextMeshProUGUI languageMainText;
	[SerializeField] private TextMeshProUGUI vsyncMainText;       // x / v
	[SerializeField] private TextMeshProUGUI vibrationMainText;   // x / v

	[Header("選項面板 (點主按鈕後打開)")]
	[SerializeField] private GameObject displayModePanel;   // 放 displayModeButtons
	[SerializeField] private GameObject resolutionPanel;    // 放 resolutionButtons
	[SerializeField] private GameObject refreshRatePanel;   // 放 refreshRateButtons
	[SerializeField] private GameObject languagePanel;      // 放 languageButtons

	[Header("Buttons (保留原本的群組按鈕陣列)")]
	[SerializeField] private Image[] displayModeButtons;
	[SerializeField] private Image[] resolutionButtons;
	[SerializeField] private Image[] refreshRateButtons;
	[SerializeField] private Image[] languageButtons;

	private List<GameObject> rootPanels = new List<GameObject>();
	private List<GameObject> optionPanels = new List<GameObject>();

	private bool isOpened = false;

	private void Start()
	{
		// Root 分頁面板清單（一般 / 員工 / 按鍵）
		rootPanels.Add(normalPanel);
		rootPanels.Add(staffPanel);
		rootPanels.Add(keyPanel);

		// 選項面板清單（顯示模式 / 解析度 / 更新率 / 語言）
		optionPanels.Add(displayModePanel);
		optionPanels.Add(resolutionPanel);
		optionPanels.Add(refreshRatePanel);
		optionPanels.Add(languagePanel);

		// 預設顯示一般分頁
		OnClickPanel(normalPanel);

		// 音量初始化（音量邏輯不動）
		InitVolumeSetting();

		settingPanel.SetActive(false);
		isOpened = false;
	}

	private void Update()
	{
		// 每次 Setting 面板被啟用時，同步顯示文字與切換狀態
		if (settingPanel.activeInHierarchy && !isOpened)
		{
			isOpened = true;
			SyncMainTextsAndTogglesFromPrefs();
			OnClickCloseAllOptionPanels();
		}
	}

	#region 初始化：音量（不動）
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

	#region OnEnable 時同步主按鈕文字與切換
	private void SyncMainTextsAndTogglesFromPrefs()
	{
		// 顯示模式
		string displayMode = PlayerPrefsManager.GetDisplayMode();
		if (displayModeMainText != null) displayModeMainText.text = displayMode;

		// 解析度
		string resolution = PlayerPrefsManager.GetResolution();
		if (resolutionMainText != null) resolutionMainText.text = resolution;

		// 更新率
		string refreshRate = PlayerPrefsManager.GetRefreshRate();
		if (refreshRateMainText != null) refreshRateMainText.text = refreshRate;

		// 語言
		string language = PlayerPrefsManager.GetLanguage();
		if (languageMainText != null) languageMainText.text = language;

		// VSync（x / v）
		bool vsync = PlayerPrefsManager.GetVSync();
		if (vsyncMainText != null) vsyncMainText.text = vsync ? "v" : "x";
		QualitySettings.vSyncCount = vsync ? 1 : 0;

		// 控制器震動（x / v）
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

	#region 音量（保持原樣）
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

	#region 顯示模式 / 解析度 / 更新率 / 語言：主按鈕 -> 打開選單
	public void OnClickOpenOptionPanel(GameObject panel)
	{
		OpenOnlyThisOptionPanel(panel);
	}

	#endregion

	#region 選單面板內：實際套用 + 更新主按鈕文字 + 關閉選單
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
		// 真正套用更新率通常需要搭配解析度或在啟動時設定，這裡僅保存與顯示
		refreshRatePanel.SetActive(false);
	}

	// -------- Language --------
	public void OnClickSelectLanguage(string lang)
	{
		PlayerPrefsManager.SetLanguage(lang);
		if (languageMainText != null) languageMainText.text = lang;
		// TODO: 依語言載入字典
		languagePanel.SetActive(false);
	}
	#endregion

	#region VSync / 震動：單一切換（x <-> v）
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

	#region 共同：分頁/面板開關（無顏色變換）
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
