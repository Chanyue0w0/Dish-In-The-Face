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
	[field: Header("--- Player SFX ---")]
	[field: Header("Player")]
	[field: SerializeField] public EventReference playerFootsteps { get; private set; }

	[field: Header("Attack")]
	[field: SerializeField] public EventReference pieAttack { get; private set; }


	[field: Header("--- Item SFX ---")]
	[field: Header("Dish")]
	[field: SerializeField] public EventReference pullDownDish { get; private set; }
	[field: Header("Coin")]
	[field: SerializeField] public EventReference coinCollected { get; private set; }
	[field: SerializeField] public EventReference beerAttack { get; private set; }
	[field: Header("--- Enemy SFX ---")]
	[field: SerializeField] public EventReference enemyAttack { get; private set; }

	public static FMODEvents Instance { get; private set; }

	private void Awake()
	{
		if (Instance != null)
		{
			Debug.LogError("Found more than one FMOD Events instance in the scene.");
		}
		Instance = this;
	}
}