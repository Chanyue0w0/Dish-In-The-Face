using UnityEngine;
using FoodsGroup;
public class FoodStatus : MonoBehaviour
{
	public FoodType foodType;
	public int Price;
	public class AttackCombo
	{
		public string animationName;

		[Min(0)] public int attackDamage = 0;
	}
}
