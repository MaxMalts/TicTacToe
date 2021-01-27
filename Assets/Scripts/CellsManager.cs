using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Assertions;



public class CellsManager : MonoBehaviour
{
	private const int fieldSize = GameManager.fieldSize;

	private CellController[][] cells = new CellController[fieldSize][] {
		new CellController[fieldSize],
		new CellController[fieldSize],
		new CellController[fieldSize]
	};

	private PlayerInput playerInput;


	public void Start()
	{
		RegisterCells();
	}



	//================= public =================

	public void SetCellSign(Vector2Int pos, CellSign sign)
	{
		Assert.IsTrue(FieldPosCorrect(pos), "Position out of range.");
		Assert.IsTrue(Enum.IsDefined(typeof(CellSign), sign), "Bad sign value.");

		cells[pos.x][pos.y].SetSign(sign);
	}


	public void EnableCellInput(Vector2Int pos)
	{
		Assert.IsTrue(FieldPosCorrect(pos), "Position out of range.");

		cells[pos.x][pos.y].EnableClickCollider();
	}


	public void DisableCellInput(Vector2Int pos)
	{
		Assert.IsTrue(FieldPosCorrect(pos), "Position out of range.");

		cells[pos.x][pos.y].DisableClickCollider();
	}


	public bool IsCell(GameObject gameObject)
	{
		return gameObject.GetComponent<CellController>() != null;
	}


	public Vector2Int CellPos(GameObject cell)
	{
		Assert.IsTrue(IsCell(cell), "Passed GameObject is not a cell.");

		return cell.GetComponent<CellController>().FieldPos;
	}



	//================= private =================

	private bool FieldPosCorrect(Vector2Int pos)
	{
		if (pos.x >= 1 && pos.x <= fieldSize &&
			pos.y >= 1 && pos.y <= fieldSize)
		{
			return true;
		}

		return false;
	}


	private void RegisterCells()
	{
		IEnumerable<CellController> allCells = FindObjectsOfType<CellController>()
			.Select(curTransform => curTransform.gameObject.GetComponent<CellController>());

		Assert.IsNotNull(cells);  // Это тест, убрать

		foreach (CellController curCell in allCells)
		{
			Vector2Int fieldPos = curCell.FieldPos;
			Assert.IsTrue(FieldPosCorrect(fieldPos), "Position out of range.");

			cells[fieldPos.x - 1][fieldPos.y - 1] = curCell;

			Assert.IsTrue(cells.All(subArr => !subArr.Contains(null)), "Field not fully filled with cells.");
		}
	}
}