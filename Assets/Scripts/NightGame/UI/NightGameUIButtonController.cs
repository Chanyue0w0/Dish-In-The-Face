using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NightGameUIButtonController : MonoBehaviour
{
	[SerializeField] GameObject settingPanel;
	public void OnClickRestart()
    {
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}
	private void Update()
	{
	}
	public void OnClickSetting()
	{
		settingPanel.SetActive(true);
	}

	public void OnClickContinue()
	{
		RoundManager.Instance.GameContinue();
	}

	public void OnClickEndNext()
	{
		SceneManager.LoadScene("Day Scene");
	}

	public void InputESC(InputAction.CallbackContext context)
	{
		if (context.started)
		{
			if (settingPanel.activeSelf)
			{
				settingPanel.SetActive(false);
				return;
			}

			RoundManager.Instance.GameStop();
		}
	}
}
