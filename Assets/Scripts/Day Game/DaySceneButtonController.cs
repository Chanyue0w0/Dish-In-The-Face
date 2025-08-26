using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using PrimeTween;

public class DaySceneButtonController : MonoBehaviour
{
	[Header("設定")]
	[SerializeField] private string sceneName = "GameScene"; // 要載入的場景名稱
	[SerializeField] private float holdDuration = 2f;        // 長按多少秒才會觸發

	[Header("UI Reference")]
	[SerializeField] private Button targetButton;            // 要監聽的按鈕
	[SerializeField] private Image fillImage;                // Fill Image (type 設為 Filled)
	[SerializeField] private LoadScenePanel loadScenePanel;  // 載入場景面板

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
		// --- 檢查 UI 按鈕 ---
		if (isHolding && !hasTriggered)
		{
			holdTimer += Time.deltaTime;
			UpdateFill();

			if (holdTimer >= holdDuration)
			{
				LoadGameScene();
			}
		}

		// --- 檢查鍵盤空白鍵輸入 ---
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

	public float score = 0;

	public AnimationCurve ScoreCurve;

	public Animator playerAnimator;

	[Button("測試換景")]
	private async void LoadGameScene()
	{
		await PrimeTween.Tween.Alpha(BlackScreen, 1.0f, 1.0f,Ease.InOutBounce).ToUniTask(PlayerLoopTiming.FixedUpdate);
		
		//撥個聲音
		await UniTask.Delay(TimeSpan.FromSeconds(5));
		
		playerAnimator.Play("獲勝動畫",0);

		await UniTask.WaitUntil(() => playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1);

		await PrimeTween.Tween.Custom(0.0f, 5500.0f, 5, SetScore,ScoreCurve);
		
		
		Debug.Log("場景載入完成");
	}

	private void SetScore(float newScore)
	{
		score = newScore;
	}
	
	[Header("載入畫面")]
	public Image BlackScreen;
	[SerializeField] private Image loadingBar; // 進度條 (設為 Filled 類型)
	[SerializeField] private GameObject loadingIcon; // Loading 圖示或動畫

	// --- Helper: 建立 EventTrigger 事件 ---
	private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
		entry.callback.AddListener((data) => action.Invoke(data));
		trigger.triggers.Add(entry);
	}
}
