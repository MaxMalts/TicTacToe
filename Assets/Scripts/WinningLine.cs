using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



[RequireComponent(typeof(LineRenderer))]
public class WinningLine : MonoBehaviour {

	[SerializeField] bool animate = true;
	public bool Animate {
		get {
			return animate;
		}

		set {
			animate = value;
		}
	}

	[SerializeField] int animationDurationMs = 200;
	public int AnimationDurationMs {
		get {
			return animationDurationMs;
		}

		set {
			animationDurationMs = value;
		}
	}

	// to place line ends further from the cell centers
	const float stretchFactor = 1.2f;

	LineRenderer lineRenderer;
	Vector2 point1 = Vector2.zero;
	Vector2 point2 = Vector2.zero;

	CellsManager cellsManager;


	public void SetLine(Vector2Int fieldPos1, Vector2Int fieldPos2) {
		CellController cell1 = cellsManager.GetCellController(fieldPos1);
		CellController cell2 = cellsManager.GetCellController(fieldPos2);

		Vector2 cellPos1 = cell1.ScenePos;
		Vector2 cellPos2 = cell2.ScenePos;

		point1 = Vector2.LerpUnclamped(cellPos1, cellPos2, 1 - stretchFactor);
		point2 = Vector2.LerpUnclamped(cellPos1, cellPos2, stretchFactor);

		Assert.IsTrue(lineRenderer.positionCount == 2,
			"Number of points on LineRenderer component not 2.");
	}

	public void Show() {
		if (animate) {
			lineRenderer.enabled = true;
			StartCoroutine(AnimateLine());

		} else {
			lineRenderer.SetPosition(0, point1);
			lineRenderer.SetPosition(1, point2);
			lineRenderer.enabled = true;
		}
	}

	public void Hide() {
		lineRenderer.enabled = false;
	}

	void Awake() {
		lineRenderer = GetComponent<LineRenderer>();
		Assert.IsNotNull(lineRenderer, "No LineRenderer component on WinningLine.");
		if (lineRenderer.enabled) {
			Debug.LogWarning("LineRenderer component of WinningLine was enabled by " +
				"default. To work correctly, please disable it before the " +
				"game starts.");
		}
	}

	void Start() {
		cellsManager = GameManager.Instance.CellsManager;
		Assert.IsNotNull(cellsManager, "No cellsManager found.");
	}

	IEnumerator AnimateLine() {
		float startTime = Time.time;
		AnimationCurve curve =
			AnimationCurve.EaseInOut(startTime, 0.0f, startTime + animationDurationMs / 1000.0f, 1.0f);

		lineRenderer.SetPosition(0, point1);
		lineRenderer.SetPosition(1, point1);

		while ((Vector2)lineRenderer.GetPosition(1) != point2) {
			float progress = curve.Evaluate(Time.time);
			Vector2 curPoint = Vector2.Lerp(point1, point2, progress);
			lineRenderer.SetPosition(1, curPoint);

			yield return new WaitForEndOfFrame();
		}
	}
}
