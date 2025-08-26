using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DaySceneButtonController : MonoBehaviour
{
	[Header("設定")]
	[SerializeField] private string sceneName = "GameScene"; // 要切換的場景
	[SerializeField] private float holdDuration = 2f;        // 長按秒數才能觸發

	[Header("UI Reference")]
	[SerializeField] private Button targetButton;            // 要監聽的按鈕
	[SerializeField] private Image fillImage;                // Fill Image (type 設成 Filled)
	[SerializeField] private LoadScenePanel loadScenePanel;  // 載入場景控制器

	private float holdTimer = 0f;
	private bool isHolding = false;
	private bool hasTriggered = false;

	private void Awake()
	{
		if (targetButton != null)
		{
			// 註冊 UI 事件
			EventTrigger trigger = targetButton.gameObject.GetComponent<EventTrigger>();
				trigger = targetButton.gameObject.AddComponent<EventTrigger>();

			// PointerDown
			AddEventTrigger(trigger, EventTriggerType.PointerDown, (data) =>
			{
				isHolding = true;
				holdTimer = 0f;
				hasTriggered = false;
				UpdateFill();
			});

			// PointerUp
			AddEventTrigger(trigger, EventTriggerType.PointerUp, (data) =>
			{
				ResetHold();
			});

			// PointerExit
			AddEventTrigger(trigger, EventTriggerType.PointerExit, (data) =>
			{
				ResetHold();
			});
		}
	}

	private void Update()
	{
		// --- 檢查 UI 長按 ---
		if (isHolding && !hasTriggered)
		{
			holdTimer += Time.deltaTime;
			UpdateFill();

			if (holdTimer >= holdDuration)
			{
				LoadGameScene();
			}
		}

		// --- 檢查鍵盤空白鍵長按 ---
		if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed && !hasTriggered)
		{
			holdTimer += Time.deltaTime;
			UpdateFill();

			if (holdTimer >= holdDuration)
			{
				LoadGameScene();
			}
		}
		else if (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
		{
			ResetHold();
		}
	}

	private void ResetHold()
	{
		isHolding = false;
		holdTimer = 0f;
			fillImage.fillAmount = 0f;
	}

	private void UpdateFill()
	{
			fillImage.fillAmount = Mathf.Clamp01(holdTimer / holdDuration);
	}

	private void LoadGameScene()
	{
		hasTriggered = true;
		Debug.Log($"Loading scene: {sceneName}");
		loadScenePanel.LoadingSceneAsync(sceneName);
	}

	// --- Helper: 建立 EventTrigger 事件 ---
	private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
		entry.callback.AddListener((data) => action.Invoke(data));
		trigger.triggers.Add(entry);
	}
}
