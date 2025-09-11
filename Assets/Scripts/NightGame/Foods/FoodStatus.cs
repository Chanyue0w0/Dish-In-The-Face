using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using FoodsGroup;
// using Spine.Unity;

public class FoodStatus : MonoBehaviour
{
    [Header("Basic Info")]
    public FoodType foodType;
    public int price;
    public float comboAttackTime;
    public EventReference defaultSfx;
    
    // [Header("Spine Reference")]
    // [SerializeField] private SkeletonAnimation skeletonAnim; // 給 Inspector 指定角色的 SkeletonAnimation

    [Header("Attack Combos")]
    public List<AttackCombo> attackList;

    [System.Serializable]
    public class AttackCombo
    {
        public AnimationClip animationClip;
        public int stunDamage;
        public EventReference sfx;
        public float knockbackForce;
        public GameObject vfxGameObject;
    }
}