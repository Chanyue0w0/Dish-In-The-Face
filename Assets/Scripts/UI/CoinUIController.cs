using System.Collections;
using UnityEngine;
using TMPro;

public class CoinUIController : MonoBehaviour
{
	[Header("coin")]
	[SerializeField] private int currentCoin = 0;
	[SerializeField] private TextMeshProUGUI coinText;
	[SerializeField] private float massageShowTime = 1.5f;
	[Header("object")]
	[SerializeField] private GameObject addCoinUI;
	[SerializeField] private GameObject subtractCoinUI;

	private void Start()
	{
		UpdateCoinText();

		if (addCoinUI != null)
			addCoinUI.SetActive(false);

		if (subtractCoinUI != null)
			subtractCoinUI.SetActive(false);
	}

	private void UpdateCoinText()
	{
		if (coinText != null)
			coinText.text = currentCoin.ToString();
	}

	public void AddCoin(int amount)
	{
		currentCoin += amount;
		UpdateCoinText();
		StartCoroutine(ShowTemporary(addCoinUI));
	}

	public void SubtractCoin(int amount)
	{
		currentCoin -= amount;
		if (currentCoin < 0) currentCoin = 0;

		UpdateCoinText();
		StartCoroutine(ShowTemporary(subtractCoinUI));
	}

	private IEnumerator ShowTemporary(GameObject uiObject)
	{
		if (uiObject == null) yield break;

		uiObject.SetActive(true);
		yield return new WaitForSeconds(massageShowTime);
		uiObject.SetActive(false);
	}
}
