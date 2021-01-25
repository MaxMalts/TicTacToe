using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class CellScript : MonoBehaviour
{

	[Tooltip("Position on board (row and column) starting from 1.")]
	public Vector2 boardPos;

	private BoxCollider2D clickCollider;


	public void Start()
	{
		clickCollider = GetComponent<BoxCollider2D>();
		Assert.IsNotNull(clickCollider);
	}


	public void DisableClick()
	{
		clickCollider.enabled = false;
	}


	public void EnableClick()
	{
		clickCollider.enabled = true;
	}
}
