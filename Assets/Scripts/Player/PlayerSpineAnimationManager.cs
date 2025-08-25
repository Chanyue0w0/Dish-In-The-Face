using UnityEngine;
using Spine.Unity;

public class PlayerSpineAnimationManager : MonoBehaviour
{
	[Header("Spine")]
	[SerializeField] private SkeletonAnimation skeletonAnim; // Character's SkeletonAnimation
	[SerializeField] private int baseTrack = 0;              // Main animation Track (usually 0)

	[Header("Animation Names")]
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string IdleFront;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string IdleBack;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunFront;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunBack;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string RunSide;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DashNormal;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DashSlide;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DrumStick1;
	[SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string DrumStick2;

	[Header("Movement Judge")]
	[SerializeField] private float idleDeadzone = 0.05f;   // Idle threshold (vector magnitude)
	[SerializeField] private float sideXThreshold = 0.05f; // X threshold for horizontal input detection
														   // Placed inside PlayerSpineAnimationManager class
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

		// Cancel all cross-animation blending (avoid transitions)
		if (skeletonAnim && skeletonAnim.AnimationState != null && skeletonAnim.AnimationState.Data != null)
			skeletonAnim.AnimationState.Data.DefaultMix = 0f;
	}

	/// <summary>Called by movement script every frame with current movement vector</summary>
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

		// ===== Prioritize side movement (if |x| exceeds threshold) =====
		if (Mathf.Abs(moveInput.x) >= sideXThreshold)
		{
			SetAnimIfChanged(RunSide, true, snap: true);
			return;
		}

		// ===== Non-side: only use back for pure backward; otherwise use front run =====
		if (moveInput.y > 0f) // Pure backward
		{
			SetAnimIfChanged(RunBack, true, snap: true);
		}
		else // Forward or diagonal forward (|x| < threshold), treat as front run
		{
			SetAnimIfChanged(RunFront, true, snap: true);
		}
	}

	/// <summary>Switch animation only if different; snap=true sets mixDuration to 0 for immediate switch</summary>
	private void SetAnimIfChanged(string animName, bool loop, bool snap)
	{
		if (currentAnimName == animName) return;

		currentAnimName = animName;
		var entry = skeletonAnim.AnimationState.SetAnimation(baseTrack, animName, loop);
		if (entry != null && snap)
		{
			entry.MixDuration = 0f; // Force immediate switch without transition
		}
	}

	/// <summary>
	/// Play a Spine animation once (non-looping) and listen for events:
	/// - "Attack_HitStart": Enable the passed hitBox
	/// - "FX_Show": Spawn vfxName at hitBox (or character if null) position, lasting 2 seconds
	/// When animation ends/is interrupted, automatically close hitBox, unlock, and call onComplete (if provided).
	/// </summary>
	public void PlayAnimationOnce(string animName, GameObject hitBox, string vfxName, System.Action onComplete = null)
	{
		if (!skeletonAnim || string.IsNullOrEmpty(animName)) return;

		// Close hitbox before starting animation
		if (hitBox) hitBox.SetActive(false);

		var entry = skeletonAnim.AnimationState.SetAnimation(baseTrack, animName, false); // Non-looping
		if (entry == null) return;

		entry.MixDuration = 0f;  // Direct switch, no transition
		isOneShotPlaying = true;

		// Events: hit start / show effects
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

		// Cleanup: ensure hitbox close, unlock and callback regardless of completion/interruption
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

	/// <summary>Flip horizontally based on x sign: x>0 faces right, x<0 faces left.</summary>
	private void SetFlipByX(float x)
	{
		//if (x > 0f)
		//	skeletonAnim.Skeleton.ScaleX = -Mathf.Abs(initialScaleX);  // Right
		//else if (x < 0f)
		//	skeletonAnim.Skeleton.ScaleX = Mathf.Abs(initialScaleX); // Left
		//															  // x≈0 no change

		//// Immediately force refresh skeleton (version compatibility)
		//skeletonAnim.Update(0f);
		//skeletonAnim.LateUpdate();
	}

	/// <summary>For front/back animations: reset X flip to initial.</summary>
	private void ResetFlipX()
	{
		//if (!Mathf.Approximately(skeletonAnim.Skeleton.ScaleX, initialScaleX))
		//{
		//	skeletonAnim.Skeleton.ScaleX = initialScaleX;
		//	// Force refresh
		//	skeletonAnim.Update(0f);
		//	skeletonAnim.LateUpdate();
		//}
	}


	public bool IsBusy() => isOneShotPlaying;
}