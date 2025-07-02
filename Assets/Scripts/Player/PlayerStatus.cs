using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
	[Header("Player Stats")]
	[SerializeField] private int maxHP = 5;       // �̤j��q
	[SerializeField] private int attackPower = 1; // �����O
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
		// �i�[�J���աG�������
		if (Input.GetKeyDown(KeyCode.H))
		{
			TakeDamage(1);
		}
	}

	// ����ˮ`
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

	// ���o�����O
	public int GetAttackPower()
	{
		return attackPower;
	}

	// ���`�B�z
	private void Die()
	{
		Debug.Log("Player has died.");
		endPane.SetActive(true);
		Time.timeScale = 0f;
		// TODO: �[�J���`�ʵe�B���s�}�l�ε����e����
	}
}
