using CodeMonkey.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid
{
    private int width;
    private int height;
	private float cellSize;

	private Vector3 originPosition;
	private int[,] gridArray;
    public Grid(int width, int height, float cellSize, Vector3 originPosition)
	{
		this.width = width;
		this.height = height;
		this.cellSize = cellSize;
		this.originPosition = originPosition;

		gridArray = new int[width, height];

		DrawCellDebugLine();
	}
	public void DrawCellDebugLine()
	{
		GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int count = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "World_Text")
            {
                Object.DestroyImmediate(obj);
                count++;
            }
        }

        Debug.Log($"Deleted {count} objects named 'World_Text'");
		for (int x = 0; x < gridArray.GetLength(0); x++)
		{
			for (int y = 0; y < gridArray.GetLength(1); y++)
			{
				UtilsClass.CreateWorldText(gridArray[x, y].ToString(), null, GetWorldPosition(x, y) + new Vector3(cellSize, cellSize) * 0.5f, 7, Color.white, TextAnchor.MiddleCenter);
				
				Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x, y + 1), Color.white, 100f);
				Debug.DrawLine(GetWorldPosition(x, y), GetWorldPosition(x + 1, y), Color.white, 100f);
			}

		}
		Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
		Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);
	}

	public Vector3 GetWorldPosition(int x, int y)
	{
		return new Vector3(x, y) * cellSize + originPosition;
	}

	

	public void GetGridXY(Vector3 worldPosition, out int x, out int y)
	{
		x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
		y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
	}

	public void SetValue(int x, int y, int value)
	{
		if (x >= 0 && y >= 0 && x < width && y < height)
		{
			gridArray[x, y] = value;
			//if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, y = y });
		}
		DrawCellDebugLine();
	}

	public void SetValue(Vector3 worldPosition, int value)
	{
		int x, y;
		GetGridXY(worldPosition, out x, out y);
		SetValue(x, y, value);
	}

	public int GetValue(int x, int y)
	{
		if (x >= 0 && y >= 0 && x < width && y < height)
		{
			return gridArray[x, y];
		}
		else
		{
			return 0;
		}
	}

	public int GetValue(Vector3 worldPosition)
	{
		int x, y;
		GetGridXY(worldPosition, out x, out y);
		return GetValue(x, y);
	}

	public bool IsPositionInGrid(int x, int y)
	{
		return x >= 0 && y >= 0 && x < width && y < height;
	}
}
