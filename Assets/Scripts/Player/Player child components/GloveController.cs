using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using Spine.Unity;

public class GloveController : MonoBehaviour
{
    [Header("Spine / Animation")]
    [SerializeField] private SkeletonAnimation skeletonAnim;
    [SerializeField] private SpineAnimationController animationController;

    [Header("Spine Animation Names")]
    [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string grabStrat;  // ※ 檔名沿用你目前的欄位命名
    [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string grabEnd;
    [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string grabbing;
    [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)] public string grabWiggle;

    [Header("Optional Attack List (Reserved)")]
    public List<AttackCombo> attackList;

    [System.Serializable]
    public class AttackCombo
    {
        public AnimationClip animationClip;
        public bool isPiercing;
        public int stunDamage;
        public EventReference sfx;
        public float knockbackForce;
        public GameObject vfxGameObject;
    }

    // ===== Public API（給 PlayerAttackController 呼叫） =====

    /// <summary>顯示手套並播放「開始抓取」的一次性動畫。</summary>
    public void ShowGloveAndPlayStart()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (animationController != null && !string.IsNullOrEmpty(grabStrat))
        {
            // 只播一次，不指定接續（等實際抓到物件時再接 grabEnd -> grabbing）
            animationController.PlayAnimation(grabStrat, false, 1f);
        }
    }

    /// <summary>已抓到物件：播放 grabEnd（一次）→ 接 grabbing（循環）。</summary>
    public void PlayGrabEndThenGrabbing()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (animationController == null) return;

        if (!string.IsNullOrEmpty(grabEnd) && !string.IsNullOrEmpty(grabbing))
        {
            animationController.PlayOnceThenLoop(grabEnd, grabbing, 1f);
        }
        else if (!string.IsNullOrEmpty(grabbing))
        {
            animationController.PlayAnimation(grabbing, true, 1f);
        }
    }

    /// <summary>直接切換到 grabbing（循環）。</summary>
    public void PlayGrabbingLoop()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (animationController != null && !string.IsNullOrEmpty(grabbing))
        {
            animationController.PlayAnimation(grabbing, true, 1f);
        }
    }

    /// <summary>
    /// 被抓物件發出「嘗試掙脫」訊息時的反應（目前先寫完並註解）。
    /// </summary>
    public void OnGrabbedObjectTryEscape()
    {
        // if (animationController != null && !string.IsNullOrEmpty(grabWiggle))
        // {
        //     // 播一次掙扎；播完可再接回 grabbing
        //     animationController.PlayOnceThenLoop(grabWiggle, grabbing, 1f);
        // }
    }

    /// <summary>物件真的掙脫：把手套隱藏。</summary>
    public void OnObjectEscaped()
    {
        if (gameObject.activeSelf) gameObject.SetActive(false);
    }
}
