using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



[RequireComponent(typeof(LineRenderer))]
public class WinningLine : MonoBehaviour {

	const float stretchFactor = 1.2f;  // to place line ends
	                                   // further from the cell centers

	LineRenderer lineRenderer;

	CellsManager cellsManager;


	public void SetLine(Vector2Int fieldPos1, Vector2Int fieldPos2) {
		CellController cell1 = cellsManager.GetCellController(fieldPos1);
		CellController cell2 = cellsManager.GetCellController(fieldPos2);

		Vector2 cellPos1 = cell1.ScenePos;
		Vector2 cellPos2 = cell2.ScenePos;

		Vector2 point1 = Vector2.LerpUnclamped(cellPos1, cellPos2, 1 - stretchFactor);
		Vector2 point2 = Vector2.LerpUnclamped(cellPos1, cellPos2, stretchFactor);

		Assert.IsTrue(lineRenderer.positionCount == 2,
			"Number of points on LineRenderer component not 2.");

		lineRenderer.SetPosition(0, point1);
		lineRenderer.SetPosition(1, point2);

		lineRenderer.enabled = true;
	}

	public void Show() {
		lineRenderer.enabled = true;
	}

	public void Hide() {
		lineRenderer.enabled = false;
	}

	void Awake() {
		lineRenderer = GetComponent<LineRenderer>();
		Assert.IsNotNull(lineRenderer, "No LineRenderer component on WinningLine.");
		if (lineRenderer.enabled) {
			Debug.LogWarning("LineRenderer component of WinningLine was enabled by " +
				"default. To work correctly, please disable it when before the " +
				"game starts.");
		}
	}

	void Start() {
		cellsManager = GameManager.Instance.CellsManager;
		Assert.IsNotNull(cellsManager, "No CellsManager found.");
	}
}
