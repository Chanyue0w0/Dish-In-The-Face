using UnityEngine;
using Spine.Unity;

public class PlayerSpineAnimationManager : MonoBehaviour
{
	[Header("Spine")]
	[SerializeField] private SkeletonAnimation skeletonAnim; // 角色的 SkeletonAnimation
	[SerializeField] private int baseTrack = 0;              // 主要動作 Track（一般用 0）

	[Header("Animation Names")]
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)]  public string IdleFront;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string IdleBack;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunFront;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunBack;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunSide;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DashNormal;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DashSlide;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DrumStick1;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DrumStick2;

	[Header("Movement Judge")]
	[SerializeField] private float idleDeadzone = 0.05f;   // 視為靜止門檻（向量長度）
	[SerializeField] private float sideXThreshold = 0.05f; // 判定「有水平輸入」的 X 門檻
	// 放到 PlayerSpineAnimationManager 類別內
	private bool isOneShotPlaying = false;

	[Header("Reference")]
	[SerializeField] private PlayerMovement playerMovement;

	private string currentAnimName = null;
	private float initialScaleX = 1f;

	private void Reset()
	{
		if (!skeletonAnim) skeletonAnim = GetComponentInChildren<SkeletonAnimation>();
	}

	private void Awake()
	{
		if (skeletonAnim && skeletonAnim.Skeleton != null)
			initialScaleX = skeletonAnim.Skeleton.ScaleX;

		// 取消所有跨動畫混合（避免漸變）
		if (skeletonAnim && skeletonAnim.AnimationState != null && skeletonAnim.AnimationState.Data != null)
			skeletonAnim.AnimationState.Data.DefaultMix = 0f;
	}

	/// <summary>由移動腳本每幀餵入目前移動向量</summary>
	public void UpdateFromMovement(Vector2 moveInput, bool _isDashingIgnored = false, bool _isSlidingIgnored = false)
	{
		if (isOneShotPlaying) return;

		if (!skeletonAnim || skeletonAnim.Skeleton == null) return;

		float sqrMag = moveInput.sqrMagnitude;
		bool isMoving = sqrMag > idleDeadzone * idleDeadzone;


		// ===== Slide =====
		if (playerMovement.IsPlayerSlide())
		{
			SetAnimIfChanged(DashSlide, true, snap: true);
			return;
		}

		// ===== Dash =====
		if (playerMovement.IsPlayerDash())
		{
			SetAnimIfChanged(DashNormal, true, snap: true);
			return;
		}

		// ===== Idle =====
		if (!isMoving)
		{
			string idleName = (moveInput.y > 0f) ? IdleBack : IdleFront;
			SetAnimIfChanged(idleName, true, snap: true);
			return;
		}

		// ===== 優先側向（只要 |x| 超過門檻就視為側向）=====
		if (Mathf.Abs(moveInput.x) >= sideXThreshold)
		{
			SetAnimIfChanged(RunSide, true, snap: true);
			return;
		}

		// ===== 非側向：只在純往後才用 back；否則用前視 run =====
		if (moveInput.y > 0f) // 純往後
		{
			SetAnimIfChanged(RunBack, true, snap: true);
		}
		else // 往前或斜前(此時 |x| < threshold)，一律視為前視 run
		{
			SetAnimIfChanged(RunFront, true, snap: true);
		}
	}

	/// <summary>如果動畫不同才切換；snap=true 會把 mixDuration 設為 0 直接切換</summary>
	private void SetAnimIfChanged(string animName, bool loop, bool snap)
	{
		if (currentAnimName == animName) return;

		currentAnimName = animName;
		var entry = skeletonAnim.AnimationState.SetAnimation(baseTrack, animName, loop);
		if (entry != null && snap)
		{
			entry.MixDuration = 0f; // 強制這次切換不做漸變
		}
	}

	/// <summary>
	/// 播放一段 Spine 動畫（不循環），並監聽事件：
	/// - "Attack_HitStart": 啟用傳入的 hitBox
	/// - "FX_Show": 於 hitBox（若為空則角色）位置生成傳入的 vfxName，生存 2 秒
	/// 動畫結束/被中斷時會自動關閉 hitBox、解除鎖定，並呼叫 onComplete（若提供）。
	/// </summary>
	public void PlayAnimationOnce(string animName, GameObject hitBox, string vfxName, System.Action onComplete = null)
	{
		if (!skeletonAnim || string.IsNullOrEmpty(animName)) return;

		// 開播前先關閉 hitbox
		if (hitBox) hitBox.SetActive(false);

		var entry = skeletonAnim.AnimationState.SetAnimation(baseTrack, animName, false); // 不循環
		if (entry == null) return;

		entry.MixDuration = 0f;  // 直接切，不做漸變
		isOneShotPlaying = true;

		// 事件：命中開始 / 顯示特效
		entry.Event += (t, e) =>
		{
			var evtName = e.Data.Name;
			if (evtName == "Attack_HitStart")
			{
				if (hitBox) hitBox.SetActive(true);
			}
			else if (evtName == "FX_Show" && vfxName != "")
			{
				var pos = hitBox ? hitBox.transform.position : skeletonAnim.transform.position;
				VFXPool.Instance.SpawnVFX(vfxName, pos, transform.rotation, 2f);
			}
		};

		// 收尾：不論完成 / 中斷都保證關閉 hitbox、解鎖與回呼
		bool closed = false;
		System.Action close = () =>
		{
			if (closed) return;
			closed = true;
			if (hitBox) hitBox.SetActive(false);
			isOneShotPlaying = false;
			onComplete?.Invoke();
		};

		entry.Complete += _ => close();
		entry.End += _ => close();
		entry.Interrupt += _ => close();
	}

	/// <summary>依 x 正負翻轉側向：x>0 面向右，x<0 面向左。</summary>
	private void SetFlipByX(float x)
	{
		//if (x > 0f)
		//	skeletonAnim.Skeleton.ScaleX = -Mathf.Abs(initialScaleX);  // 右
		//else if (x < 0f)
		//	skeletonAnim.Skeleton.ScaleX = Mathf.Abs(initialScaleX); // 左
		//															  // x≈0 不改變

		//// 立刻強制刷新骨架（版本相容）
		//skeletonAnim.Update(0f);
		//skeletonAnim.LateUpdate();
	}

	/// <summary>前/後動畫用：把 X 翻轉還原為初始。</summary>
	private void ResetFlipX()
	{
		//if (!Mathf.Approximately(skeletonAnim.Skeleton.ScaleX, initialScaleX))
		//{
		//	skeletonAnim.Skeleton.ScaleX = initialScaleX;
		//	// 強制刷新
		//	skeletonAnim.Update(0f);
		//	skeletonAnim.LateUpdate();
		//}
	}


	public bool IsBusy() => isOneShotPlaying;
}
