using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WroldGridController : MonoBehaviour
{
	[Header("Map Grid cell setting")]
	[SerializeField] private int gridWidth;
	[SerializeField] private int gridHeight;
	[SerializeField] private float cellSize;
	[SerializeField] private Vector3 originPosition;

	[Header("Highlight")]
	[SerializeField] private GameObject highlightSquare;

	private Grid grid;
	private Vector3 lastHighlightPos = Vector3.positiveInfinity;

	void Start()
	{
		grid = new Grid(gridWidth, gridHeight, cellSize, originPosition);
		highlightSquare.transform.localScale = Vector3.one * cellSize;
		highlightSquare.SetActive(false);
	}

}
