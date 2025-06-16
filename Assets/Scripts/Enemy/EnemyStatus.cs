using UnityEngine;

public class EnemyStatus : MonoBehaviour
{
	[SerializeField] private int hp;   // 1~3
	[SerializeField] private int atk;  // 1~10
	[SerializeField] private int def;  // 0~10

	private void Awake()
	{
		hp = Random.Range(1, 4);     // �]�t 3
		atk = Random.Range(1, 11);   // �]�t 10
		def = Random.Range(0, 11);   // �]�t 10
	}

	// Getter
	public int GetHP() => hp;
	public int GetATK() => atk;
	public int GetDEF() => def;

	// Setter
	public void SetHP(int value)
	{
		hp = Mathf.Clamp(value, 1, 3);
	}

	public void SetATK(int value)
	{
		atk = Mathf.Clamp(value, 1, 10);
	}

	public void SetDEF(int value)
	{
		def = Mathf.Clamp(value, 0, 10);
	}

	// 1. �s�W GetHurt ���
	public void GetHurt(int damage = 1)
	{
		hp -= damage;
		Debug.Log($"Enemy got hurt! Remaining HP: {hp}");

		if (hp <= 0)
		{
			Die();
		}
	}

	private void Die()
	{
		Debug.Log("Enemy died!");
		Destroy(gameObject);
	}

	// 2. �Y�I�� Tag �� Player �� Trigger�A�� 1 �w��
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player"))
		{
			Debug.Log("Enemy get hurt!");
			GetHurt(1);
		}
	}
}
