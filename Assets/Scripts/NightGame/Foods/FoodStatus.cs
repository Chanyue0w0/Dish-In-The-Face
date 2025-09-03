using System.Collections.Generic;
using UnityEngine;
using FoodsGroup;
using Spine.Unity;

public class FoodStatus : MonoBehaviour
{
    [Header("Basic Info")]
    public FoodType foodType;
    public int price;

    [Header("Spine Reference")]
    [SerializeField] private SkeletonAnimation skeletonAnim; // 給 Inspector 指定角色的 SkeletonAnimation

    [Header("Attack Combos")]
    public List<AttackCombo> attackList;

    [System.Serializable]
    public class AttackCombo
    {
        [SpineAnimation(dataField: "skeletonAnim", fallbackToTextField: true)]
        public string animationName;

        public int attackDamage;
    }
}