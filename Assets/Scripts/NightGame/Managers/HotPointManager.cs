using UnityEngine;
using UnityEngine.UI;

public class HotPointManager : MonoBehaviour
{
	[SerializeField] private float hotPoint = 0f;              // 0~10 ����
	[Header("-------- Setting ---------")]
	[SerializeField] private float decreaseRate = 0.1f;        // �C��U��������
	[SerializeField] private float decayDelay = 3f;            // ���ݦh�֬��}�l�U��
	[SerializeField] private float maxHotPoint = 10f;

	[Header("-------- Reference ---------")]
	[SerializeField] private Sprite[] hotLevelSprites;         // ���� 0~4 ���� D~S
	[SerializeField] private Image hotPointImage;              // �ϥܤ���
	[SerializeField] private Image hotPointFillBar;            // fillAmount�������
	[SerializeField] private Color[] hotLevelColors;           // ���� D~S ���C��]��������5�^

	private float lastHotIncreaseTime;

	private void Start()
	{
		UpdateHotUI();
	}

	private void Update()
	{
		if (Time.time - lastHotIncreaseTime > decayDelay)
		{
			if (hotPoint > 0f)
			{
				hotPoint -= decreaseRate * Time.deltaTime;
				hotPoint = Mathf.Clamp(hotPoint, 0f, maxHotPoint);
				UpdateHotUI();
			}
		}
	}

	public void AddHotPoint(float value)
	{
		hotPoint += value;
		hotPoint = Mathf.Clamp(hotPoint, 0f, maxHotPoint);
		lastHotIncreaseTime = Time.time;
		//Debug.Log("hotPoint :" + hotPoint);

		UpdateHotUI();
	}

	private void UpdateHotUI()
	{
		int levelIndex = GetHotLevelIndex();

		// �����ϥ�
		if (hotLevelSprites != null && hotLevelSprites.Length > levelIndex)
			hotPointImage.sprite = hotLevelSprites[levelIndex];

		// ��s��R�q�P�C��
		hotPointFillBar.fillAmount = hotPoint / maxHotPoint;

		//if (hotLevelColors != null && hotLevelColors.Length > levelIndex)
		//	hotPointFillBar.color = hotLevelColors[levelIndex];
	}

	public int GetMoneyMultiplier()
	{
		return GetHotLevelIndex() + 1;
	}

	private int GetHotLevelIndex()
	{
		if (hotPoint >= 8f) return 4; // S
		if (hotPoint >= 6f) return 3; // A
		if (hotPoint >= 4f) return 2; // B
		if (hotPoint >= 2f) return 1; // C
		return 0;                     // D
	}
}
