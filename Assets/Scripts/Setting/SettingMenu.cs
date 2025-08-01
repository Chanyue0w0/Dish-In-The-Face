using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class SettingMenu : MonoBehaviour
{
	[Header("Panels")]
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

	[Header("Buttons")]
	[SerializeField] private Image[] displayModeButtons;
	[SerializeField] private Image[] resolutionButtons;
	[SerializeField] private Image[] vsyncButtons;
	[SerializeField] private Image[] refreshRateButtons;
	[SerializeField] private Image[] vibrationButtons;
	[SerializeField] private Image[] languageButtons;

	private List<GameObject> panels = new List<GameObject>();
	private void Start()
	{
		settingPanel.SetActive(false);
		panels.Add(normalPanel);
		panels.Add(staffPanel);
		panels.Add(keyPanel);

		OnClickPanel(normalPanel);
		InitSetting();
	}

	private void InitSetting()
	{
		// 音量設定
		float master = PlayerPrefsManager.GetMasterVolume();
		masterSlider.value = master;
		OnValueChangedMasterVolume();

		float music = PlayerPrefsManager.GetMusicVolume();
		musicSlider.value = music;
		OnValueChangedMusicVolume();

		float sfx = PlayerPrefsManager.GetSFXVolume();
		sfxSlider.value = sfx;
		OnValueChangedSFXVolume();

		// 顯示模式
		string displayMode = PlayerPrefsManager.GetDisplayMode();
		OnClickSetDisplayMode(displayMode);

		// 解析度
		string resolution = PlayerPrefsManager.GetResolution();
		OnClickSetResolution(resolution);

		// VSync
		bool vsync = PlayerPrefsManager.GetVSync();
		OnClickSetVSync(vsync);

		// 更新率
		string refreshRate = PlayerPrefsManager.GetRefreshRate();
		OnClickSetRefreshRate(refreshRate);

		// 控制器震動
		bool vibration = PlayerPrefsManager.GetControllerVibration();
		OnClickSetControllerVibration(vibration);

		// 語言
		string language = PlayerPrefsManager.GetLanguage();
		OnClickSetLanguage(language);
	}

	// Exit
	public void OnClickExit()
	{
		settingPanel.SetActive(false);
	}

	// -------- Master Volume --------
	public void OnValueChangedMasterVolume()
	{
		masterVolumeText.text = ((int)(masterSlider.value * 100f)).ToString();
		PlayerPrefsManager.SetMasterVolume(masterSlider.value);
		AudioManager.instance.masterVolume = masterSlider.value;
	}
	public void OnClickMasterVolume(bool increase)
	{
		masterSlider.value += increase ? 0.01f : -0.01f;
		masterSlider.value = Mathf.Clamp(masterSlider.value, 0f, 1f);
		OnValueChangedMasterVolume();
	}

	// -------- Music Volume --------
	public void OnValueChangedMusicVolume()
	{
		musicVolumeText.text = ((int)(musicSlider.value * 100f)).ToString();
		PlayerPrefsManager.SetMusicVolume(musicSlider.value);
		AudioManager.instance.musicVolume = musicSlider.value;
	}
	public void OnClickMusicVolume(bool increase)
	{
		musicSlider.value += increase ? 0.01f : -0.01f;
		musicSlider.value = Mathf.Clamp(musicSlider.value, 0f, 1f);
		OnValueChangedMusicVolume();
	}

	// -------- SFX Volume --------
	public void OnValueChangedSFXVolume()
	{
		sfxVolumeText.text = ((int)(sfxSlider.value * 100f)).ToString();
		PlayerPrefsManager.SetSFXVolume(sfxSlider.value);
		AudioManager.instance.SFXVolume = sfxSlider.value;
	}
	public void OnClickSFXVolume(bool increase)
	{
		sfxSlider.value += increase ? 0.01f : -0.01f;
		sfxSlider.value = Mathf.Clamp(sfxSlider.value, 0f, 1f);
		OnValueChangedSFXVolume();
	}

	// -------- Display Mode --------
	public void OnClickSetDisplayMode(string mode)
	{
		HighlightButtonGroup(displayModeButtons, EventSystem.current.currentSelectedGameObject);
		PlayerPrefsManager.SetDisplayMode(mode);
		if (mode == "Borderless Windowed") Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
		else if (mode == "Windowed Mode") Screen.fullScreenMode = FullScreenMode.Windowed;
		else if (mode == "Fullscreen") Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
	}

	// -------- Resolution --------
	public void OnClickSetResolution(string resolution)
	{
		HighlightButtonGroup(resolutionButtons, EventSystem.current.currentSelectedGameObject);
		PlayerPrefsManager.SetResolution(resolution);
		string[] parts = resolution.Split('x');
		Screen.SetResolution(int.Parse(parts[0]), int.Parse(parts[1]), Screen.fullScreenMode);
	}

	// -------- V-Sync --------
	public void OnClickSetVSync(bool isOn)
	{
		HighlightButtonGroup(vsyncButtons, EventSystem.current.currentSelectedGameObject);
		PlayerPrefsManager.SetVSync(isOn);
		QualitySettings.vSyncCount = isOn ? 1 : 0;
	}

	// -------- Refresh Rate --------
	public void OnClickSetRefreshRate(string rate)
	{
		HighlightButtonGroup(refreshRateButtons, EventSystem.current.currentSelectedGameObject);
		PlayerPrefsManager.SetRefreshRate(rate);
		// 功能應與解析度一併實作
	}

	// -------- Controller Vibration --------
	public void OnClickSetControllerVibration(bool isOn)
	{
		HighlightButtonGroup(vibrationButtons, EventSystem.current.currentSelectedGameObject);
		PlayerPrefsManager.SetControllerVibration(isOn);
		RumbleManager.Instance.SetEnableRumble(isOn);
	}

	// -------- Language（保留功能區） --------
	public void OnClickSetLanguage(string lang)
	{
		HighlightButtonGroup(languageButtons, EventSystem.current.currentSelectedGameObject);
		PlayerPrefsManager.SetLanguage(lang);
		// TODO: 載入語言字典
	}

	// -------- 按鈕高亮 --------
	private void HighlightButtonGroup(Image[] buttonImages, GameObject currentButtonOgj)
	{
		for (int i = 0; i < buttonImages.Length; i++)
		{
			buttonImages[i].color = (buttonImages[i].gameObject == currentButtonOgj) ? Color.red : Color.white;
		}
	}

	public void OnClickPanel(GameObject panel)
	{
		foreach(GameObject p in panels)
		{
			if (panel == p) p.SetActive(true);
			else p.SetActive(false);
		}
	}
}