using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GameManager : MonoBehaviour
{

	public const int fieldSize = 3;

	public GameObject cells;

	private int[,] fieldState = new int[fieldSize, fieldSize];


    void Start()
    {
		//cells.AddCellClickCallback(CellClickHandler);
    }


	void CellClickHandler(Vector2 pos)
	{

	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
