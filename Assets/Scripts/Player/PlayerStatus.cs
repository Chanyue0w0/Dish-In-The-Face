using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
	[Header("Player Stats")]
	[SerializeField] private int maxHP = 5;       // 最大血量
	[SerializeField] private int attackPower = 1; // 攻擊力
	[SerializeField] private GameObject endPane;
	[SerializeField] private HealthPointUIControll healthPointUIControl;
	[SerializeField] private GameObject getHurtVFX;

	private int currentHP;

	// Start is called before the first frame update
	void Start()
	{
		currentHP = maxHP;
	}

	// Update is called once per frame
	void Update()
	{
		// 可加入測試：按鍵受傷
		if (Input.GetKeyDown(KeyCode.H))
		{
			TakeDamage(1);
		}
	}

	// 受到傷害
	public void TakeDamage(int damage)
	{

		RumbleManager.Instance.Rumble(0.7f, 0.7f, 0.5f);

		currentHP -= damage;
		currentHP = Mathf.Clamp(currentHP, 0, maxHP);

		//Debug.Log("Player took damage. Current HP: " + currentHP);

		Instantiate(getHurtVFX, transform.position, Quaternion.identity);
		healthPointUIControl.DecreaseHP();
		if (currentHP <= 0)
		{
			Die();
		}
	}

	// 取得攻擊力
	public int GetAttackPower()
	{
		return attackPower;
	}

	// 死亡處理
	private void Die()
	{
		Debug.Log("Player has died.");
		endPane.SetActive(true);
		Time.timeScale = 0f;
		// TODO: 加入死亡動畫、重新開始或結束畫面等
	}
}
