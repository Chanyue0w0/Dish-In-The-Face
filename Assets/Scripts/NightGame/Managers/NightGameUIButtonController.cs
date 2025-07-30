using UnityEngine;
using UnityEngine.SceneManagement;

public class NightGameUIButtonController : MonoBehaviour
{
	[SerializeField] GameObject settingPanel;
	[SerializeField] RoundManager roundManager;
    public void OnClickRestart()
    {
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	public void OnClickSetting()
	{
		settingPanel.SetActive(true);
	}

	public void OnClickContinue()
	{
		roundManager.GameContinue();
	}
}
