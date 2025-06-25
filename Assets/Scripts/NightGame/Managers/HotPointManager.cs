using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HotPointManager : MonoBehaviour
{
	[SerializeField] private float hotPoint = 0f;          // 0~10 ����
	[Header("-------- Setting ---------")]
	[SerializeField] private float decreaseRate = 0.1f;    // �C��U��������
	[SerializeField] private float decayDelay = 3f;        // ���ݦh�֬��}�l�U��
	[SerializeField] private float maxHotPoint = 10f;


	[Header("-------- Reference ---------")]
	[SerializeField] private Sprite[] hotLevelSprites;     // ���� 0~4 ���� D~S
	[SerializeField] private Image hotPointImage;

	private float lastHotIncreaseTime;                     // �W�����׼W�[���ɶ�

	private void Start()
	{
		UpdateHotLevelSprite();
	}

	private void Update()
	{
		// �Y�W�L decayDelay ��S���W�[���סA�h�}�l�U��
		if (Time.time - lastHotIncreaseTime > decayDelay)
		{
			if (hotPoint > 0f)
			{
				hotPoint -= decreaseRate * Time.deltaTime;
				hotPoint = Mathf.Clamp(hotPoint, 0f, maxHotPoint);
				UpdateHotLevelSprite();
			}
		}
	}

	/// �W�[����
	public void AddHotPoint(float value)
	{
		hotPoint += value;
		hotPoint = Mathf.Clamp(hotPoint, 0f, maxHotPoint);
		Debug.Log("hotPoint :" + hotPoint);
		lastHotIncreaseTime = Time.time;
		UpdateHotLevelSprite();
	}

	/// �ھڼ��׳]�w�������ϥ�
	private void UpdateHotLevelSprite()
	{
		int levelIndex = GetHotLevelIndex();
		if (hotLevelSprites != null && hotLevelSprites.Length > levelIndex)
		{
			hotPointImage.sprite = hotLevelSprites[levelIndex];
		}
	}

	/// �ھڼ��ר��o���v�]1~5 ���^
	public int GetMoneyMultiplier()
	{
		return GetHotLevelIndex() + 1;
	}

	/// �ھڼ��ר��o���ů��� 0(D), 1(C), ..., 4(S)
	private int GetHotLevelIndex()
	{
		if (hotPoint >= 8f) return 4; // S
		if (hotPoint >= 6f) return 3; // A
		if (hotPoint >= 4f) return 2; // B
		if (hotPoint >= 2f) return 1; // C
		return 0;                     // D
	}
}
