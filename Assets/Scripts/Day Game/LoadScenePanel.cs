using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
public class LoadScenePanel : MonoBehaviour
{
	[Header("UI Reference")]
	[SerializeField] private GameObject loadingPanel;  // Loading 面板
	[SerializeField] private Image fillImage;          // 進度條 (Image type 設為 Filled)
	[SerializeField] private TextMeshProUGUI progressText;        // 百分比文字（可選）

	private void Awake()
	{
		loadingPanel.SetActive(false); // 遊戲一開始隱藏
		fillImage.fillAmount = 0f;
	}

	/// <summary>
	/// 呼叫此函式以顯示 loading panel 並開始載入
	/// </summary>
	/// <param name="sceneName">要載入的場景名稱</param>
	public void LoadingSceneAsync(string sceneName)
	{
		if (loadingPanel != null)
			loadingPanel.SetActive(true);

		StartCoroutine(LoadSceneAsync(sceneName));
	}

	/// <summary>
	/// 非同步載入場景並更新進度條
	/// </summary>
	private IEnumerator LoadSceneAsync(string sceneName)
	{
		AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
		operation.allowSceneActivation = false; // 不要自動切換，等我們控制

		while (!operation.isDone)
		{
			// Unity 的 progress 最大是 0.9，剩下 0.1 是準備階段
			float progress = Mathf.Clamp01(operation.progress / 0.9f);

			// 更新進度條
			if (fillImage != null)
				fillImage.fillAmount = progress;

			if (progressText != null)
				progressText.text = Mathf.RoundToInt(progress * 100f) + "%";

			// 當進度到 90%（progress == 1f）
			if (progress >= 1f)
			{
				// 等待 0.5 秒，讓玩家看進度滿了
				yield return new WaitForSeconds(0.5f);
				operation.allowSceneActivation = true;
			}

			yield return null;
		}
	}
}
