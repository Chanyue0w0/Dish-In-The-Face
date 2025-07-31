using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class NightGameUIButtonController : MonoBehaviour
{
	[SerializeField] GameObject settingPanel;
	[SerializeField] RoundManager roundManager;
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
		roundManager.GameContinue();
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

			roundManager.GameStop();
		}
	}
}
