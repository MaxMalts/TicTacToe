using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Assertions;



public class CellsManager : Unique<CellsManager> {

	public List<CellSign> PlaceAudioCellSigns { get; set; }

	const int fieldSize = GameManager.fieldSize;

	[SerializeField] AudioSource placingAudio;

	CellController[][] cells;

	PlayerInput playerInput;
	bool cellsRegistered = false;


	public CellController GetCellController(Vector2Int pos) {
		Assert.IsTrue(FieldPosInRange(pos), "Position out of range.");

		if (!cellsRegistered) {
			RegisterCells();
			cellsRegistered = true;
		}
		return cells[pos.x - 1][pos.y - 1];
	}

	public void SetCellSign(Vector2Int pos, CellSign sign) {
		Assert.IsTrue(FieldPosInRange(pos), "Position out of range.");
		Assert.IsTrue(Enum.IsDefined(typeof(CellSign), sign), "Bad sign value.");

		if (!cellsRegistered) {
			RegisterCells();
			cellsRegistered = true;
		}

		CellController curCell = cells[pos.x - 1][pos.y - 1];
		if (curCell.Sign != sign) {
			cells[pos.x - 1][pos.y - 1].SetSign(sign);

			if (placingAudio != null &&
				PlaceAudioCellSigns != null &&
				PlaceAudioCellSigns.Contains(sign)) {

				placingAudio.Play();
			}
		}
	}

	public CellSign GetCellSign(Vector2Int pos) {
		Assert.IsTrue(FieldPosInRange(pos), "Position out of range.");

		if (!cellsRegistered) {
			RegisterCells();
			cellsRegistered = true;
		}
		return cells[pos.x - 1][pos.y - 1].Sign;
	}

	public void EnableCellInput(Vector2Int pos) {
		Assert.IsTrue(FieldPosInRange(pos), "Position out of range.");

		if (!cellsRegistered) {
			RegisterCells();
			cellsRegistered = true;
		}
		cells[pos.x - 1][pos.y - 1].EnableClickCollider();
	}

	public void DisableCellInput(Vector2Int pos) {
		Assert.IsTrue(FieldPosInRange(pos), "Position out of range.");

		if (!cellsRegistered) {
			RegisterCells();
			cellsRegistered = true;
		}
		cells[pos.x - 1][pos.y - 1].DisableClickCollider();
	}

	public bool IsCell(GameObject gameObject) {
		return gameObject.GetComponent<CellController>() != null;
	}

	public Vector2Int CellPos(GameObject cell) {
		Assert.IsTrue(IsCell(cell), "Passed GameObject is not a cell.");

		return cell.GetComponent<CellController>().FieldPos;
	}

	public bool FieldPosInRange(Vector2Int pos) {
		if (pos.x >= 1 && pos.x <= fieldSize &&
			pos.y >= 1 && pos.y <= fieldSize) {
			return true;
		}

		return false;
	}

	public int CalcCellsBySign(CellSign sign) {
		if (!cellsRegistered) {
			RegisterCells();
			cellsRegistered = true;
		}

		int res = 0;
		foreach(CellController[] curColumn in cells) {
			foreach (CellController curCell in curColumn) {
				if (curCell.Sign == sign) {
					++res;
				}
			}
		}

		return res;
	}

	public void ResetAllCells() {
		if (!cellsRegistered) {
			RegisterCells();
			cellsRegistered = true;
		}

		foreach (CellController[] curColumn in cells) {
			foreach (CellController curCell in curColumn) {
				curCell.SetSign(CellSign.Empty);
				curCell.EnableClickCollider();
			}
		}
	}

	void Awake() {
		cells = new CellController[fieldSize][];
		for (int i = 0; i < fieldSize; ++i) {
			cells[i] = new CellController[fieldSize];
		}

		if (placingAudio == null) {
			Debug.LogWarning("PlacingAudio was not assigned in inspector.");
		}
	}

	void Start() {
		if (!cellsRegistered) {
			RegisterCells();
			cellsRegistered = true;
		}
	}

	void RegisterCells() {
		IEnumerable<CellController> allCells = FindObjectsOfType<CellController>()
			.Select(curTransform => curTransform.gameObject.GetComponent<CellController>());

		foreach (CellController curCell in allCells) {
			Vector2Int fieldPos = curCell.FieldPos;
			Assert.IsTrue(FieldPosInRange(fieldPos), "Position out of range.");

			cells[fieldPos.x - 1][fieldPos.y - 1] = curCell;
		}

		Assert.IsTrue(cells.All(subArr => !subArr.Contains(null)), "Field not fully filled with cells.");
	}
}