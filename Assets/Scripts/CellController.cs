﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



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

	public CellSign Sign { get; private set; } = CellSign.Empty;


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
			case CellSign.Empty: {
				Assert.IsTrue(transform.childCount == 0, "Cell Sign is CellSign.Empty but it has children.");

				GameObject oldChild = transform.GetChild(0).gameObject;
				Destroy(oldChild);
				break;
			}

			case CellSign.Cross:
			case CellSign.Nought: {
				Assert.IsTrue(transform.childCount <= 1, "Cell has more than one child.");

				if (transform.childCount != 0) {
					GameObject oldChild = transform.GetChild(0).gameObject;
					Destroy(oldChild);
				}

				Instantiate(newSign == CellSign.Cross ? crossPrefab : noughtPrefab, transform);
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