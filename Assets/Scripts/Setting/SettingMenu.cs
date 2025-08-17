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

	[Header("Main Buttons Text (��ܷ�e�]�w��)")]
	[SerializeField] private TextMeshProUGUI displayModeMainText;
	[SerializeField] private TextMeshProUGUI resolutionMainText;
	[SerializeField] private TextMeshProUGUI refreshRateMainText;
	[SerializeField] private TextMeshProUGUI languageMainText;
	[SerializeField] private TextMeshProUGUI vsyncMainText;       // x / v
	[SerializeField] private TextMeshProUGUI vibrationMainText;   // x / v

	[Header("�ﶵ���O (�I�D���s�ᥴ�})")]
	[SerializeField] private GameObject displayModePanel;   // �� displayModeButtons
	[SerializeField] private GameObject resolutionPanel;    // �� resolutionButtons
	[SerializeField] private GameObject refreshRatePanel;   // �� refreshRateButtons
	[SerializeField] private GameObject languagePanel;      // �� languageButtons

	[Header("Buttons (�O�d�쥻���s�ի��s�}�C)")]
	[SerializeField] private Image[] displayModeButtons;
	[SerializeField] private Image[] resolutionButtons;
	[SerializeField] private Image[] refreshRateButtons;
	[SerializeField] private Image[] languageButtons;

	private List<GameObject> rootPanels = new List<GameObject>();
	private List<GameObject> optionPanels = new List<GameObject>();

	private bool isOpened = false;

	private void Start()
	{
		// Root �������O�M��]�@�� / ���u / ����^
		rootPanels.Add(normalPanel);
		rootPanels.Add(staffPanel);
		rootPanels.Add(keyPanel);

		// �ﶵ���O�M��]��ܼҦ� / �ѪR�� / ��s�v / �y���^
		optionPanels.Add(displayModePanel);
		optionPanels.Add(resolutionPanel);
		optionPanels.Add(refreshRatePanel);
		optionPanels.Add(languagePanel);

		// �w�]��ܤ@�����
		OnClickPanel(normalPanel);

		// ���q��l�ơ]���q�޿褣�ʡ^
		InitVolumeSetting();

		settingPanel.SetActive(false);
		isOpened = false;
	}

	private void Update()
	{
		// �C�� Setting ���O�Q�ҥήɡA�P�B��ܤ�r�P�������A
		if (settingPanel.activeInHierarchy && !isOpened)
		{
			isOpened = true;
			SyncMainTextsAndTogglesFromPrefs();
			OnClickCloseAllOptionPanels();
		}
	}

	#region ��l�ơG���q�]���ʡ^
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

	#region OnEnable �ɦP�B�D���s��r�P����
	private void SyncMainTextsAndTogglesFromPrefs()
	{
		// ��ܼҦ�
		string displayMode = PlayerPrefsManager.GetDisplayMode();
		if (displayModeMainText != null) displayModeMainText.text = displayMode;

		// �ѪR��
		string resolution = PlayerPrefsManager.GetResolution();
		if (resolutionMainText != null) resolutionMainText.text = resolution;

		// ��s�v
		string refreshRate = PlayerPrefsManager.GetRefreshRate();
		if (refreshRateMainText != null) refreshRateMainText.text = refreshRate;

		// �y��
		string language = PlayerPrefsManager.GetLanguage();
		if (languageMainText != null) languageMainText.text = language;

		// VSync�]x / v�^
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

	#region ���q�]�O����ˡ^
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
	public void OnClickSelectLanguage(string lang)
	{
		PlayerPrefsManager.SetLanguage(lang);
		if (languageMainText != null) languageMainText.text = lang;
		// TODO: �̻y�����J�r��
		languagePanel.SetActive(false);
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
