using System.Collections.Generic;
using UnityEngine;
using FoodsGroup;
public class FoodStatus : MonoBehaviour
{
	public FoodType foodType;
	public int price;

	public List<AttackCombo> attackList;
	
	[System.Serializable]
	public class AttackCombo
	{
		public string animationName;

		public int attackDamage = 0;
	}
}
