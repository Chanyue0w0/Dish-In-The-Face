using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RumbleManager : MonoBehaviour
{
	public static RumbleManager Instance { get; private set; }

	private Coroutine rumbleCoroutine;
	private bool wasTimeScaleZero = false;

	private bool isRumble = true;

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
		//  檢查 timeScale 是否變為 0
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

	/// 啟動震動（指定震度與持續時間）
	public void Rumble(float lowFrequency, float highFrequency, float duration)
	{
		if (!isRumble) return;

		if (Gamepad.current == null) return;

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

	/// 啟動持續震動（無限時長，需手動停止）
	public void RumbleContinuous(float lowFrequency, float highFrequency)
	{
		if (!isRumble) return;


		if (Gamepad.current == null) return;

		if (rumbleCoroutine != null)
			StopCoroutine(rumbleCoroutine);

		Gamepad.current.SetMotorSpeeds(lowFrequency, highFrequency);
		rumbleCoroutine = null; // 不需記錄 Coroutine，讓 StopRumble 照常運作
	}

	/// 停止震動
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
}