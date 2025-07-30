using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
	//[field: Header("Ambience")]
	//[field: SerializeField] public EventReference ambience { get; private set; }

	[field: Header("----------- Music -----------")]
	[field: SerializeField] public EventReference music { get; private set; }


	[field: Header("----------- SFX -----------")]
	[field: Header("Player SFX")]
	[field: SerializeField] public EventReference playerFootsteps { get; private set; }

	[field: Header("Coin SFX")]
	[field: SerializeField] public EventReference coinCollected { get; private set; }
	[field: Header("Attack SFX")]
	[field: SerializeField] public EventReference pieAttack { get; private set; }
	[field: SerializeField] public EventReference beerAttack { get; private set; }
	[field: Header("Enemy SFX")]
	[field: SerializeField] public EventReference enemyAttack { get; private set; }

	public static FMODEvents instance { get; private set; }

	private void Awake()
	{
		if (instance != null)
		{
			Debug.LogError("Found more than one FMOD Events instance in the scene.");
		}
		instance = this;
	}
}