using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Assertions;



public class LocalPlayerInput : MonoBehaviour
{
	[SerializeField] private new Camera camera;

	private PlayerInput playerInput;
	private PlayerController playerController;

	public CellsManager cellsManager;


	public void Awake()
	{
		playerInput = GetComponent<PlayerInput>();
		Assert.IsNotNull(playerInput, "No Player Input Component on Player.");

		playerController = GetComponent<PlayerController>();
		Assert.IsNotNull(playerInput, "No Player Controller script on Player.");
	}



	//================= public =================

	public void EnableInput()
	{
		playerInput.enabled = true;
	}


	public void DisableInput()
	{
		playerInput.enabled = false;
	}


	public void OnTap(InputAction.CallbackContext context)
	{
		Vector2 coords = context.ReadValue<Vector2>();
		Ray ray = camera.ScreenPointToRay(coords);

		RaycastHit hit;
		Physics.Raycast(ray, out hit);

		GameObject hitObject = hit.transform.gameObject;

		if (cellsManager.IsCell(hitObject))
		{
			playerController.Place(cellsManager.CellPos(hitObject));
		}
	}
}
