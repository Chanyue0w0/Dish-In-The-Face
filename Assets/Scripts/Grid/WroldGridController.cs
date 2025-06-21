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

	void Update()
	{
		Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		mouseWorldPos.z = 0;

		grid.GetGridXY(mouseWorldPos, out int x, out int y);

		Vector3 cellWorldPos = grid.GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * 0.5f;

		if (grid.IsPositionInGrid(x, y))
		{
			if (cellWorldPos != lastHighlightPos)
			{
				highlightSquare.SetActive(true);
				highlightSquare.transform.position = cellWorldPos;
				lastHighlightPos = cellWorldPos;
			}
		}
		else
		{
			highlightSquare.SetActive(false);
		}
	}
}
