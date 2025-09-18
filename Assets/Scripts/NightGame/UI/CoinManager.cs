using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
	[SerializeField] private int currentCoin = 0;
	[Header("---------- Setting ----------")]
	[SerializeField] private float massageShowTime = 1.5f;
	[Header("---------- Reference ----------")]
	[SerializeField] private TextMeshProUGUI coinText;
	[SerializeField] private TextMeshProUGUI endCoinText;
	[SerializeField] private GameObject addCoinUI;
	[SerializeField] private GameObject subtractCoinUI;

	private void Start()
	{
		UpdateCoinText();

		if (addCoinUI != null)
			addCoinUI.SetActive(false);

		if (subtractCoinUI != null)
			subtractCoinUI.SetActive(false);
		
		endCoinText.gameObject.SetActive(true);
	}

	private void Update()
	{
		if (endCoinText != null)
			endCoinText.text = coinText.text;
	}

	private void UpdateCoinText()
	{
		if (coinText != null)
			coinText.text = currentCoin.ToString();
	}

	public void AddCoin(int amount)
	{
		currentCoin += amount;

		addCoinUI.GetComponent<TextMeshProUGUI>().text = "+ " + amount.ToString();
		UpdateCoinText();
		StartCoroutine(ShowTemporary(addCoinUI));
	}

	// public void SubtractCoin(int amount)
	// {
	// 	currentCoin -= amount;
	// 	if (currentCoin < 0) currentCoin = 0;
	//
	// 	subtractCoinUI.GetComponent<TextMeshProUGUI>().text = "-" + amount.ToString();
	// 	UpdateCoinText();
	// 	StartCoroutine(ShowTemporary(subtractCoinUI));
	// }

	private IEnumerator ShowTemporary(GameObject uiObject)
	{
		if (uiObject == null) yield break;

		uiObject.SetActive(true);
		yield return new WaitForSeconds(massageShowTime);
		uiObject.SetActive(false);
	}
}