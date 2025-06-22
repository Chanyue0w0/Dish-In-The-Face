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
	[SerializeField] private bool isEditorMode;

	[Header("Reference")]
	[SerializeField] private GameObject highlightSquarePreferb;
	[SerializeField] private GameObject furniturePreferb;

	// Grid 資料結構，用來儲存地圖格子資料
	private Grid grid;

	// 新增家具變數
	private GameObject previewFurnitureInstance;
	private List<GameObject> highlightInstances = new List<GameObject>();
	// 記錄上次高亮格的位置，用來避免重複產生 highlight
	private Vector3 lastHighlightPos = Vector3.positiveInfinity;

	void Start()
	{
		// 初始化 Grid
		grid = new Grid(gridWidth, gridHeight, cellSize, originPosition);

		// 調整 highlight prefab 的大小，並隱藏
		highlightSquarePreferb.transform.localScale = Vector3.one * cellSize;
		highlightSquarePreferb.SetActive(false);


	}


	void Update()
	{
		if (isEditorMode)
		{

			Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			mouseWorldPosition.z = 0;

			grid.GetGridXY(mouseWorldPosition, out int cellX, out int cellY);
			Vector3 alignedPosition = grid.GetWorldPosition(cellX, cellY);

			FurniturePlacePreview(alignedPosition);
			// 滑鼠左鍵點擊 -> 放置家具
			if (Input.GetMouseButtonDown(0) && previewFurnitureInstance != null)
			{
				PlaceNewFurniture(alignedPosition);
			}
		}


	}

	private void FurniturePlacePreview(Vector3 alignedPosition)
	{
		// 若滑鼠移出原本 cell
		if (alignedPosition != lastHighlightPos)
		{
			lastHighlightPos = alignedPosition;
			if (previewFurnitureInstance != null) // 已存在選取的家具
			{
				previewFurnitureInstance.transform.position = alignedPosition;

			}
			else CreatFurniturePreview(alignedPosition);
		}
	}

	private void CreatFurniturePreview(Vector3 alignedPosition)
	{
		// resect
		// 刪除前一次生成的家具與 highlight
		if (previewFurnitureInstance != null)
		{
			Destroy(previewFurnitureInstance);
		}
		foreach (GameObject go in highlightInstances)
		{
			Destroy(go);
		}
		highlightInstances.Clear();

		// 生成新的預覽與 highlight
		// 生成家具預覽
		previewFurnitureInstance = Instantiate(furniturePreferb, alignedPosition, Quaternion.identity);
		previewFurnitureInstance.gameObject.name += " preview";
		previewFurnitureInstance.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);

		FurnitureState furnitureState = previewFurnitureInstance.GetComponent<FurnitureState>();
		Vector3 furnitureScale = previewFurnitureInstance.transform.localScale;  // 或是直接拿 spriteRenderer.size
		Vector3 spriteSize = previewFurnitureInstance.GetComponent<SpriteRenderer>().bounds.size; // 拿 sprite 的世界單位尺寸

		// grid.cellSize 表示 1 格的世界單位尺寸
		int sizeX = Mathf.RoundToInt(spriteSize.x / cellSize);
		int sizeY = Mathf.RoundToInt(spriteSize.y / cellSize);

		// 設定
		furnitureState.sizeOfCell = new Vector2Int(sizeX, sizeY);  // 2D 遊戲通常 Z = 1

		// highlight生成起始點於 previewFurnitureInstance 左下角
		float offsetX = -(sizeX - 1) * 0.5f * cellSize;
		float offsetY = -(sizeY - 1) * 0.5f * cellSize;
		Vector3 leftBottom = alignedPosition + new Vector3(offsetX, offsetY, 0);

		for (int x = 0; x < sizeX; x++)
		{
			for (int y = 0; y < sizeY; y++)
			{
				Vector3 cellPos = leftBottom + new Vector3(x * cellSize, y * cellSize, 0);
				GameObject highlight = Instantiate(highlightSquarePreferb, cellPos, Quaternion.identity, previewFurnitureInstance.transform);
				highlight.SetActive(true);
				highlightInstances.Add(highlight);
			}
		}
	}

	private void PlaceNewFurniture(Vector3 alignedPosition)
	{
		// 1. 基於 Cell位置生成 newFurnitureObject
		GameObject newFurnitureObject = Instantiate(furniturePreferb, alignedPosition, Quaternion.identity);
		newFurnitureObject.gameObject.name += " Object";
		newFurnitureObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);

		FurnitureState furnitureState = newFurnitureObject.GetComponent<FurnitureState>();
		Vector3 furnitureScale = newFurnitureObject.transform.localScale;  // 或是直接拿 spriteRenderer.size
		Vector3 spriteSize = newFurnitureObject.GetComponent<SpriteRenderer>().bounds.size; // 拿 sprite 的世界單位尺寸

		// grid.cellSize 表示 1 格的世界單位尺寸
		int sizeX = Mathf.RoundToInt(spriteSize.x / cellSize);
		int sizeY = Mathf.RoundToInt(spriteSize.y / cellSize);

		// 設定
		furnitureState.sizeOfCell = new Vector2Int(sizeX, sizeY);  // 2D 遊戲通常 Z = 1

		int id = furnitureState.ID;

		// 2. 更改 value(家具所站的所有 cells 都要更改)
	}
}
