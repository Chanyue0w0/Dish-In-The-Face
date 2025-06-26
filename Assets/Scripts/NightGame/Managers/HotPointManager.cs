using UnityEngine;
using UnityEngine.UI;

public class HotPointManager : MonoBehaviour
{
	[SerializeField] private float hotPoint = 0f;              // 0~10 之間
	[Header("-------- Setting ---------")]
	[SerializeField] private float decreaseRate = 0.1f;        // 每秒下降的熱度
	[SerializeField] private float decayDelay = 3f;            // 等待多少秒後開始下降
	[SerializeField] private float maxHotPoint = 10f;

	[Header("-------- Reference ---------")]
	[SerializeField] private Sprite[] hotLevelSprites;         // 索引 0~4 對應 D~S
	[SerializeField] private Image hotPointImage;              // 圖示切換
	[SerializeField] private Image hotPointFillBar;            // fillAmount控制熱度
	[SerializeField] private Color[] hotLevelColors;           // 對應 D~S 的顏色（長度應為5）

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

		// 切換圖示
		if (hotLevelSprites != null && hotLevelSprites.Length > levelIndex)
			hotPointImage.sprite = hotLevelSprites[levelIndex];

		// 更新填充量與顏色
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
