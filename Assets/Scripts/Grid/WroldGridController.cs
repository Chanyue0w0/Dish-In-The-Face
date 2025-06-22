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

	// Grid ��Ƶ��c�A�Ψ��x�s�a�Ϯ�l���
	private Grid grid;

	// �s�W�a���ܼ�
	private GameObject previewFurnitureInstance;
	private List<GameObject> highlightInstances = new List<GameObject>();
	// �O���W�����G�檺��m�A�Ψ��קK���Ʋ��� highlight
	private Vector3 lastHighlightPos = Vector3.positiveInfinity;

	void Start()
	{
		// ��l�� Grid
		grid = new Grid(gridWidth, gridHeight, cellSize, originPosition);

		// �վ� highlight prefab ���j�p�A������
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
			// �ƹ������I�� -> ��m�a��
			if (Input.GetMouseButtonDown(0) && previewFurnitureInstance != null)
			{
				PlaceNewFurniture(alignedPosition);
			}
		}


	}

	private void FurniturePlacePreview(Vector3 alignedPosition)
	{
		// �Y�ƹ����X�쥻 cell
		if (alignedPosition != lastHighlightPos)
		{
			lastHighlightPos = alignedPosition;
			if (previewFurnitureInstance != null) // �w�s�b������a��
			{
				previewFurnitureInstance.transform.position = alignedPosition;

			}
			else CreatFurniturePreview(alignedPosition);
		}
	}

	private void CreatFurniturePreview(Vector3 alignedPosition)
	{
		// resect
		// �R���e�@���ͦ����a��P highlight
		if (previewFurnitureInstance != null)
		{
			Destroy(previewFurnitureInstance);
		}
		foreach (GameObject go in highlightInstances)
		{
			Destroy(go);
		}
		highlightInstances.Clear();

		// �ͦ��s���w���P highlight
		// �ͦ��a��w��
		previewFurnitureInstance = Instantiate(furniturePreferb, alignedPosition, Quaternion.identity);
		previewFurnitureInstance.gameObject.name += " preview";
		previewFurnitureInstance.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);

		FurnitureState furnitureState = previewFurnitureInstance.GetComponent<FurnitureState>();
		Vector3 furnitureScale = previewFurnitureInstance.transform.localScale;  // �άO������ spriteRenderer.size
		Vector3 spriteSize = previewFurnitureInstance.GetComponent<SpriteRenderer>().bounds.size; // �� sprite ���@�ɳ��ؤo

		// grid.cellSize ��� 1 �檺�@�ɳ��ؤo
		int sizeX = Mathf.RoundToInt(spriteSize.x / cellSize);
		int sizeY = Mathf.RoundToInt(spriteSize.y / cellSize);

		// �]�w
		furnitureState.sizeOfCell = new Vector2Int(sizeX, sizeY);  // 2D �C���q�` Z = 1

		// highlight�ͦ��_�l�I�� previewFurnitureInstance ���U��
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
		// 1. ��� Cell��m�ͦ� newFurnitureObject
		GameObject newFurnitureObject = Instantiate(furniturePreferb, alignedPosition, Quaternion.identity);
		newFurnitureObject.gameObject.name += " Object";
		newFurnitureObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);

		FurnitureState furnitureState = newFurnitureObject.GetComponent<FurnitureState>();
		Vector3 furnitureScale = newFurnitureObject.transform.localScale;  // �άO������ spriteRenderer.size
		Vector3 spriteSize = newFurnitureObject.GetComponent<SpriteRenderer>().bounds.size; // �� sprite ���@�ɳ��ؤo

		// grid.cellSize ��� 1 �檺�@�ɳ��ؤo
		int sizeX = Mathf.RoundToInt(spriteSize.x / cellSize);
		int sizeY = Mathf.RoundToInt(spriteSize.y / cellSize);

		// �]�w
		furnitureState.sizeOfCell = new Vector2Int(sizeX, sizeY);  // 2D �C���q�` Z = 1

		int id = furnitureState.ID;

		// 2. ��� value(�a��ү����Ҧ� cells ���n���)
	}
}
