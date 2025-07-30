using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingMenu : MonoBehaviour
{
	// -------- UI 參考 --------

	[Header("-------- UI Reference --------")]
	[Header("Panels")]
	[SerializeField] private GameObject settingPanel;
	[Header("Volume UI")]
	[SerializeField] private Slider masterSlider;
	[SerializeField] private TextMeshProUGUI masterVolumeText;

	[SerializeField] private Slider musicSlider;
	[SerializeField] private TextMeshProUGUI musicVolumeText;

	[SerializeField] private Slider sfxSlider;
	[SerializeField] private TextMeshProUGUI sfxVolumeText;

	[Header("Buttons")]
	[SerializeField] private Button[] displayModeButtons;
	[SerializeField] private Button[] resolutionButtons;
	[SerializeField] private Button[] vsyncButtons;
	[SerializeField] private Button[] refreshRateButtons;
	[SerializeField] private Button[] vibrationButtons;
	[SerializeField] private Button[] languageButtons;

	private void Start()
	{
		settingPanel.SetActive(false);
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
		for (int i = 0; i < displayModeButtons.Length; i++)
		{
			bool isSelected = displayModeButtons[i].name == mode;
			ColorBlock colors = displayModeButtons[i].colors;
			colors.normalColor = isSelected ? Color.red : Color.white;
			displayModeButtons[i].colors = colors;
		}
		PlayerPrefsManager.SetDisplayMode(mode);
		if (mode == "Borderless Windowed") Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
		else if (mode == "Windowed Mode") Screen.fullScreenMode = FullScreenMode.Windowed;
		else if (mode == "Fullscreen") Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
	}

	// -------- Resolution --------
	public void OnClickSetResolution(string resolution)
	{
		for (int i = 0; i < resolutionButtons.Length; i++)
		{
			bool isSelected = resolutionButtons[i].name == resolution;
			ColorBlock colors = resolutionButtons[i].colors;
			colors.normalColor = isSelected ? Color.red : Color.white;
			resolutionButtons[i].colors = colors;
		}
		PlayerPrefsManager.SetResolution(resolution);
		string[] parts = resolution.Split('x');
		Screen.SetResolution(int.Parse(parts[0]), int.Parse(parts[1]), Screen.fullScreenMode);
	}

	// -------- V-Sync --------
	public void OnClickSetVSync(bool isOn)
	{
		HighlightButtonGroup(vsyncButtons, isOn ? 0 : 1);
		PlayerPrefsManager.SetVSync(isOn);
		QualitySettings.vSyncCount = isOn ? 1 : 0;
	}

	// -------- Refresh Rate --------
	public void OnClickSetRefreshRate(string rate)
	{
		for (int i = 0; i < refreshRateButtons.Length; i++)
		{
			bool isSelected = refreshRateButtons[i].name == rate;
			ColorBlock colors = refreshRateButtons[i].colors;
			colors.normalColor = isSelected ? Color.red : Color.white;
			refreshRateButtons[i].colors = colors;
		}
		PlayerPrefsManager.SetRefreshRate(rate);
		// 功能應與解析度一併實作
	}

	// -------- Controller Vibration --------
	public void OnClickSetControllerVibration(bool isOn)
	{
		HighlightButtonGroup(vibrationButtons, isOn ? 0 : 1);
		PlayerPrefsManager.SetControllerVibration(isOn);
		RumbleManager.Instance.SetEnableRumble(isOn);
	}

	// -------- Language（保留功能區） --------
	public void OnClickSetLanguage(string lang)
	{
		for (int i = 0; i < languageButtons.Length; i++)
		{
			bool isSelected = languageButtons[i].name == lang;
			ColorBlock colors = languageButtons[i].colors;
			colors.normalColor = isSelected ? Color.red : Color.white;
			languageButtons[i].colors = colors;
		}
		PlayerPrefsManager.SetLanguage(lang);
		// TODO: 載入語言字典
	}

	// -------- 按鈕高亮 --------
	private void HighlightButtonGroup(Button[] buttons, int selectedIndex)
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			ColorBlock colors = buttons[i].colors;
			colors.normalColor = (i == selectedIndex) ? Color.red : Color.white;
			buttons[i].colors = colors;
		}
	}
}