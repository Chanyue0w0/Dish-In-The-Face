using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WroldGridController : MonoBehaviour
{
    [SerializeField] private int gridWidth = 20;
	[SerializeField] private int gridHeight = 10;
	[SerializeField] private float cellSize = 1;

    //[SerializeField] private bool isShowGridLine;
    // Start is called before the first frame update

    Grid grid;
	void Start()
    {
        grid = new Grid(gridWidth, gridHeight, cellSize);
    }

    // Update is called once per frame
    void Update()
    {
        //if (isShowGridLine)
        //{
        //    grid.DrawCellDebugLine();
        //}
    }
}
