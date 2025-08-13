using UnityEngine;

/// <summary>
/// 專責玩家動畫與面向控制：
/// - 根據 X 方向設定角色左右面向（Y 軸 0/180）
/// - 根據移動/衝刺/滑行狀態設定 Animator 參數
/// </summary>
public class PlayerAnimationManager : MonoBehaviour
{
	[Header("Reference")]
	[SerializeField] private Animator animator;              // 角色 Animator
	[SerializeField] private Transform characterTransform;   // 用於翻面（通常就是玩家本體 Transform）

	[Header("Animator Params")]
	[SerializeField] private string walkParam = "isWalk";
	[SerializeField] private string dashParam = "isDash";
	[SerializeField] private string slideParam = "isSlide";

	private void Reset()
	{
		// 在編輯器中新增元件時嘗試自動抓取
		if (!animator) animator = GetComponentInChildren<Animator>();
		if (!characterTransform) characterTransform = transform;
	}

	/// 對外單點更新（由 Movement 呼叫）：餵入目前移動向量與狀態
	public void UpdateFromMovement(Vector2 moveInput, bool isDashing, bool isSliding)
	{
		UpdateFacing(moveInput.x);
		UpdateStates(moveInput, isDashing, isSliding);
	}

	/// 只更新面向（可在讀取輸入當下就呼叫）
	public void UpdateFacing(float moveX)
	{
		if (!characterTransform) return;

		if (moveX < -Mathf.Epsilon)
			characterTransform.rotation = Quaternion.Euler(0f, 180f, 0f);
		else if (moveX > Mathf.Epsilon)
			characterTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
	}

	/// 依狀態切換 Animator 參數：滑行 > 衝刺 > 走路 > 待機（互斥）
	public void UpdateStates(Vector2 moveInput, bool isDashing, bool isSliding)
	{
		if (!animator) return;

		bool isWalk = moveInput != Vector2.zero;

		if (isSliding)
		{
			SetBool(walkParam, false);
			SetBool(dashParam, false);
			SetBool(slideParam, true);
		}
		else if (isDashing)
		{
			SetBool(walkParam, false);
			SetBool(dashParam, true);
			SetBool(slideParam, false);
		}
		else if (isWalk)
		{
			SetBool(walkParam, true);
			SetBool(dashParam, false);
			SetBool(slideParam, false);
		}
		else
		{
			SetBool(walkParam, false);
			SetBool(dashParam, false);
			SetBool(slideParam, false);
		}
	}

	private void SetBool(string param, bool value)
	{
		if (!string.IsNullOrEmpty(param))
			animator.SetBool(param, value);
	}
}
