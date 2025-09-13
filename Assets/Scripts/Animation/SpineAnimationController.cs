using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class SpineAnimationController : MonoBehaviour
{
    [Header("Spine Reference")]
    [SerializeField] private SkeletonAnimation skeletonAnim;

    [Header("Default / Current Animation")]
    [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)]
    [SerializeField] private string defaultAnimation = string.Empty;

    [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)]
    [SerializeField] private string currentAnimation = string.Empty;

    [Header("Settings")]
    [Tooltip("預設使用的 Track 索引（大多用 0）。")]
    [SerializeField] private int trackIndex = 0;

    [Tooltip("設定 SetAnimation / AddAnimation 的混合延遲（秒）。通常 0 即可，或交由 Spine Mix 設定。")]
    [SerializeField, Min(0f)] private float mixDelay = 0f;

    [Tooltip("一次性動畫結束後是否要自動回到循環動畫")]
    [SerializeField] private bool returnToLoopAfterOnce = true;

    // 狀態紀錄
    private bool currentLoop = true;
    private string lastLoopingAnimation = string.Empty; 
    private bool initialized;

    #region Unity Lifecycle
    private void Awake()
    {
        TryInitializeSkeleton();
    }

    private void Start()
    {
        if (IsStateEmpty())
        {
            PlayAnimation(string.IsNullOrEmpty(defaultAnimation) ? currentAnimation : defaultAnimation, true);
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && skeletonAnim != null && skeletonAnim.SkeletonDataAsset != null)
        {
            var data = skeletonAnim.SkeletonDataAsset.GetSkeletonData(false);
            if (data != null && string.IsNullOrEmpty(defaultAnimation) && data.Animations.Count > 0)
            {
                defaultAnimation = data.Animations.Items[0].Name;
            }
        }
    }
    #endregion

    #region Public API
    public List<string> GetAnimationNames()
    {
        var names = new List<string>();
        if (skeletonAnim == null || skeletonAnim.SkeletonDataAsset == null) return names;

        var data = skeletonAnim.SkeletonDataAsset.GetSkeletonData(false);
        if (data == null || data.Animations == null) return names;

        foreach (var anim in data.Animations)
            names.Add(anim.Name);

        return names;
    }

    public void PlayAnimation(string animName, bool loop, float timeScale = 1f)
    {
        if (!TryInitializeSkeleton()) return;
        SetAnimIfChanged(animName, loop, timeScale);
    }

    public void PlayOnceThenReturn(string oneShotAnim, float timeScale = 1f)
    {
        if (!TryInitializeSkeleton()) return;

        // 決定回去後要接的動畫
        string backAnim = ResolveReturnLoopingAnimation();

        if (currentLoop && IsValidAnimation(currentAnimation))
        {
            lastLoopingAnimation = currentAnimation;
        }

        var state = skeletonAnim.AnimationState;
        var entry = state.SetAnimation(trackIndex, oneShotAnim, false);
        if (entry == null)
        {
            if (returnToLoopAfterOnce) FallbackToDefaultLoop();
            return;
        }

        skeletonAnim.timeScale = Mathf.Max(0f, timeScale);
        currentAnimation = oneShotAnim;
        currentLoop = false;

        // 只有在 returnToLoopAfterOnce 為 true 時才接回循環動畫
        if (returnToLoopAfterOnce)
        {
            if (!string.IsNullOrEmpty(backAnim) && IsValidAnimation(backAnim))
            {
                state.AddAnimation(trackIndex, backAnim, true, mixDelay);
                currentAnimation = backAnim;
                currentLoop = true;
            }
            else
            {
                FallbackToDefaultLoop();
            }
        }
    }

    /// <summary>
    /// 新增：明確指定「一次性動畫播完 → 指定 loop 動畫」的流程。
    /// </summary>
    public void PlayOnceThenLoop(string oneShotAnim, string loopAnim, float timeScale = 1f)
    {
        if (!TryInitializeSkeleton()) return;

        if (!IsValidAnimation(oneShotAnim))
        {
            // 一次性動畫無效就直接播 loopAnim（若有效）
            if (IsValidAnimation(loopAnim))
                PlayAnimation(loopAnim, true, timeScale);
            else
                FallbackToDefaultLoop();
            return;
        }

        // 若指定的 loopAnim 有效，先把它記成最後的循環動畫
        if (IsValidAnimation(loopAnim))
            lastLoopingAnimation = loopAnim;

        var state = skeletonAnim.AnimationState;
        var entry = state.SetAnimation(trackIndex, oneShotAnim, false);
        if (entry == null)
        {
            FallbackToDefaultLoop();
            return;
        }

        skeletonAnim.timeScale = Mathf.Max(0f, timeScale);
        currentAnimation = oneShotAnim;
        currentLoop = false;

        if (IsValidAnimation(loopAnim))
        {
            state.AddAnimation(trackIndex, loopAnim, true, mixDelay);
            currentAnimation = loopAnim;
            currentLoop = true;
        }
        else
        {
            FallbackToDefaultLoop();
        }
    }

    public void SetAnimIfChanged(string animName, bool loop, float timeScale = 1f)
    {
        if (!TryInitializeSkeleton()) return;

        if (!IsValidAnimation(animName))
        {
            FallbackToDefaultLoop();
            return;
        }

        if (currentAnimation == animName && currentLoop == loop) return;

        var state = skeletonAnim.AnimationState;

        if (loop) lastLoopingAnimation = animName;

        var entry = state.SetAnimation(trackIndex, animName, loop);
        if (entry == null)
        {
            FallbackToDefaultLoop();
            return;
        }

        skeletonAnim.timeScale = Mathf.Max(0f, timeScale);
        currentAnimation = animName;
        currentLoop = loop;
    }

    public void SetTimeScale(float timeScale)
    {
        if (!TryInitializeSkeleton()) return;
        skeletonAnim.timeScale = Mathf.Max(0f, timeScale);
    }
    #endregion

    #region Helpers
    private bool TryInitializeSkeleton()
    {
        if (skeletonAnim == null) return false;

        if (!initialized)
        {
            if (!skeletonAnim.valid)
            {
                skeletonAnim.Initialize(true);
            }
            initialized = true;
        }
        return skeletonAnim.valid && skeletonAnim.AnimationState != null;
    }

    private bool IsValidAnimation(string animName)
    {
        if (string.IsNullOrEmpty(animName)) return false;
        if (skeletonAnim == null || skeletonAnim.SkeletonDataAsset == null) return false;

        var data = skeletonAnim.SkeletonDataAsset.GetSkeletonData(false);
        if (data == null) return false;

        return data.FindAnimation(animName) != null;
    }

    private bool IsStateEmpty()
    {
        if (!TryInitializeSkeleton()) return true;
        var state = skeletonAnim.AnimationState;
        var track = state.GetCurrent(trackIndex);
        return track == null || string.IsNullOrEmpty(track.Animation?.Name);
    }

    private string ResolveReturnLoopingAnimation()
    {
        if (currentLoop && IsValidAnimation(currentAnimation))
            return currentAnimation;

        if (!string.IsNullOrEmpty(lastLoopingAnimation) && IsValidAnimation(lastLoopingAnimation))
            return lastLoopingAnimation;

        if (!string.IsNullOrEmpty(defaultAnimation) && IsValidAnimation(defaultAnimation))
            return defaultAnimation;

        return string.Empty;
    }

    private void FallbackToDefaultLoop()
    {
        if (!TryInitializeSkeleton()) return;

        string fallback = string.Empty;

        if (!string.IsNullOrEmpty(defaultAnimation) && IsValidAnimation(defaultAnimation))
            fallback = defaultAnimation;
        else
        {
            var names = GetAnimationNames();
            if (names.Count > 0) fallback = names[0];
        }

        if (!string.IsNullOrEmpty(fallback))
        {
            var state = skeletonAnim.AnimationState;
            var entry = state.SetAnimation(trackIndex, fallback, true);
            if (entry != null)
            {
                currentAnimation = fallback;
                currentLoop = true;
                lastLoopingAnimation = fallback;
            }
        }
    }
    #endregion
}
