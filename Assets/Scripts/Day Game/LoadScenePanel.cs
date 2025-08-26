using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadScenePanel : MonoBehaviour
{
	[Header("UI Reference")]
	[SerializeField] private GameObject loadingPanel;  // Loading ���O
	[SerializeField] private Image fillImage;          // �i�ױ� (Image type �]�� Filled)
	[SerializeField] private TextMeshProUGUI progressText;        // �ʤ����r�]�i��^

	private void Awake()
	{
		loadingPanel.SetActive(false); // �C���@�}�l����
		fillImage.fillAmount = 0f;
	}

	/// <summary>
	/// �I�s���\�� �� ��� loading panel �ö}�l���J
	/// </summary>
	/// <param name="sceneName">�n�����������W��</param>
	public void LoadingSceneAsync(string sceneName)
	{
		if (loadingPanel != null)
			loadingPanel.SetActive(true);

		StartCoroutine(LoadSceneAsync(sceneName));
	}

	/// <summary>
	/// �D�P�B���J�����ç�s�i�ױ�
	/// </summary>
	private IEnumerator LoadSceneAsync(string sceneName)
	{
		AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
		operation.allowSceneActivation = false; // ���n�۰ʤ����A���ڭ̱���

		while (!operation.isDone)
		{
			// Unity �� progress �̤j�� 0.9�A�ѤU 0.1 �O�E������
			float progress = Mathf.Clamp01(operation.progress / 0.9f);

			// ��s�i�ױ�
			if (fillImage != null)
				fillImage.fillAmount = progress;

			if (progressText != null)
				progressText.text = Mathf.RoundToInt(progress * 100f) + "%";

			// ��i�ר� 90%�]progress == 1f�^
			if (progress >= 1f)
			{
				// ���� 0.5 ��A�����a�ݶi�׺���
				yield return new WaitForSeconds(0.5f);
				operation.allowSceneActivation = true;
			}

			yield return null;
		}
	}
}
