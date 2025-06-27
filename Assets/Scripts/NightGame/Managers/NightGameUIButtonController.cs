using UnityEngine;
using UnityEngine.SceneManagement;

public class NightGameUIButtonController : MonoBehaviour
{
    public void OnClickRestart()
    {
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		Time.timeScale = 1.0f;
	}
}
