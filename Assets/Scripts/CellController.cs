using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


public enum CellSign {
	empty,
	cross,
	nought
}


public class CellController : MonoBehaviour {

	[Tooltip("Position on field (row and column) starting from 1.")]
	[SerializeField] Vector2Int fieldPos;
	public Vector2Int FieldPos {
		get {
			return fieldPos;
		}
	}

	[SerializeField] BoxCollider2D clickCollider;

	[SerializeField] GameObject crossPrefab;
	[SerializeField] GameObject noughtPrefab;

	public CellSign Sign { get; private set; } = CellSign.empty;


	public void Start() {
		clickCollider = GetComponent<BoxCollider2D>();
		Assert.IsNotNull(clickCollider);
	}


	public void DisableClickCollider() {
		clickCollider.enabled = false;
	}


	public void EnableClickCollider() {
		clickCollider.enabled = true;
	}


	public void SetSign(CellSign newSign) {

		if (newSign == Sign) {
			return;
		}

		switch (newSign) {
			case CellSign.empty: {
				Assert.IsTrue(transform.childCount == 0, "Cell Sign is CellSign.empty but it has children.");

				GameObject oldChild = transform.GetChild(0).gameObject;
				Destroy(oldChild);
				break;
			}

			case CellSign.cross: {
				Assert.IsTrue(transform.childCount <= 1, "Cell has more than one child.");

				if (transform.childCount != 0) {
					GameObject oldChild = transform.GetChild(0).gameObject;
					Destroy(oldChild);
				}

				Instantiate(crossPrefab, transform);
				break;
			}

			case CellSign.nought: {
				Assert.IsTrue(transform.childCount <= 1, "Cell has more than one child.");

				GameObject oldChild = transform.GetChild(0)?.gameObject;
				if (oldChild != null)
					Destroy(oldChild);

				Instantiate(noughtPrefab, transform);
				break;
			}

			default: {
				Assert.IsTrue(false, "Bad newSign value.");
				break;
			}
		}

		Sign = newSign;
	}
}