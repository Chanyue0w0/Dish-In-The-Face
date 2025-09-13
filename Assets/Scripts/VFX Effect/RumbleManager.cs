using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleManager : MonoBehaviour
{
	public static RumbleManager Instance { get; private set; }

	private Coroutine rumbleCoroutine;
	private bool wasTimeScaleZero = false;

	[SerializeField] private bool isRumble = true;
	[Range(0f, 1f)]
	[SerializeField] private float rumbleScale = 1f; // �_�ʱj�פ�ҡ]0~1�^

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
	}

	private void Update()
	{
		// �ˬd timeScale �O�_�ܬ� 0
		if (Time.timeScale == 0f && !wasTimeScaleZero)
		{
			StopRumble();
			wasTimeScaleZero = true;
		}
		else if (Time.timeScale > 0f && wasTimeScaleZero)
		{
			wasTimeScaleZero = false;
		}
	}

	/// �Ұʾ_�ʡ]���w�_�׻P����ɶ��^
	public void Rumble(float lowFrequency, float highFrequency, float duration)
	{
		if (!isRumble) return;
		if (Gamepad.current == null) return;

		// �ھ� rumbleScale �վ�_�ʱj��
		lowFrequency *= rumbleScale;
		highFrequency *= rumbleScale;

		if (rumbleCoroutine != null)
			StopCoroutine(rumbleCoroutine);

		rumbleCoroutine = StartCoroutine(RumbleRoutine(lowFrequency, highFrequency, duration));
	}

	private IEnumerator RumbleRoutine(float lowFreq, float highFreq, float duration)
	{
		Gamepad.current.SetMotorSpeeds(lowFreq, highFreq);
		yield return new WaitForSeconds(duration);
		Gamepad.current.SetMotorSpeeds(0f, 0f);
		rumbleCoroutine = null;
	}

	/// �Ұʫ���_�ʡ]�L���ɪ��A�ݤ�ʰ���^
	public void RumbleContinuous(float lowFrequency, float highFrequency)
	{
		if (!isRumble) return;
		if (Gamepad.current == null) return;

		if (rumbleCoroutine != null)
			StopCoroutine(rumbleCoroutine);

		// �ھ� rumbleScale �վ�_�ʱj��
		lowFrequency *= rumbleScale;
		highFrequency *= rumbleScale;

		Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
		rumbleCoroutine = null;
	}

	/// ����_��
	public void StopRumble()
	{
		if (Gamepad.current == null) return;

		if (rumbleCoroutine != null)
			StopCoroutine(rumbleCoroutine);

		Gamepad.current.SetMotorSpeeds(0f, 0f);
		rumbleCoroutine = null;
	}

	private void OnDisable()
	{
		StopRumble();
	}

	private void OnApplicationQuit()
	{
		StopRumble();
	}

	public void SetEnableRumble(bool rumble)
	{
		isRumble = rumble;
	}

	/// �]�w�_�ʤ�ҡ]0~1�^
	public void SetRumbleScale(float scale)
	{
		rumbleScale = Mathf.Clamp01(scale);
	}
}
