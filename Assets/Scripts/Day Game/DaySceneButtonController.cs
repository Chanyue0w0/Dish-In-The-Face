using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DaySceneButtonController : MonoBehaviour
{
	[Header("�]�w")]
	[SerializeField] private string sceneName = "GameScene"; // �n����������
	[SerializeField] private float holdDuration = 2f;        // ������Ƥ~��Ĳ�o

	[Header("UI Reference")]
	[SerializeField] private Button targetButton;            // �n��ť�����s
	[SerializeField] private Image fillImage;                // Fill Image (type �]�� Filled)
	[SerializeField] private LoadScenePanel loadScenePanel;  // ���J�������

	private float holdTimer = 0f;
	private bool isHolding = false;
	private bool hasTriggered = false;

	private void Awake()
	{
		if (targetButton != null)
		{
			// ���U UI �ƥ�
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
		// --- �ˬd UI ���� ---
		if (isHolding && !hasTriggered)
		{
			holdTimer += Time.deltaTime;
			UpdateFill();

			if (holdTimer >= holdDuration)
			{
				LoadGameScene();
			}
		}

		// --- �ˬd��L�ť������ ---
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

	// --- Helper: �إ� EventTrigger �ƥ� ---
	private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
		entry.callback.AddListener((data) => action.Invoke(data));
		trigger.triggers.Add(entry);
	}
}
