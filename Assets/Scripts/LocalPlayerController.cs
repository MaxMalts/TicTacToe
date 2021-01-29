using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Assertions;



public class LocalPlayerController : MonoBehaviour {

	[SerializeField] private new Camera camera;

	private PlayerInput playerInput;
	private PlayerAPI playerController;

	public CellsManager cellsManager;

	[SerializeField] private InputActionAsset actionsAsset;
	private InputAction pointerPosAction;


	public void Awake() {
		playerInput = GetComponent<PlayerInput>();
		Assert.IsNotNull(playerInput, "No Player Input Component on Player.");

		playerController = GetComponent<PlayerAPI>();
		Assert.IsNotNull(playerInput, "No PlayerAPI script on Player.");
	}


	public void Start() {
		pointerPosAction = actionsAsset.FindAction("Screen Interactions/Position", true);
	}


	public void Update() {
		if (Mouse.current != null) {
			InputSystem.EnableDevice(Mouse.current);
		}
		if (Pointer.current != null) {
			InputSystem.EnableDevice(Pointer.current);
		}
		if (Touchscreen.current != null) {
			InputSystem.EnableDevice(Touchscreen.current);
		}
	}



	//================= public =================

	public void EnableInput() {
		playerInput.enabled = true;
	}


	public void DisableInput() {
		playerInput.enabled = false;
	}


	public void OnPointerRelease(InputAction.CallbackContext context) {

		if (context.phase != InputActionPhase.Performed) {
			return;
		}
		if (pointerPosAction.phase != InputActionPhase.Started) {
			return;
		}

		Debug.Log("Pointer Release: " + context.ToString());

		Vector2 pointerPos = pointerPosAction.ReadValue<Vector2>();
		Vector2 worldPos = camera.ScreenToWorldPoint(pointerPos);

		RaycastHit2D hitInfo = Physics2D.Raycast(worldPos, Vector2.zero, 0);
		if (hitInfo.collider == null) {
			Debug.Log("Nothing hit.");
			return;
		}

		GameObject hitObject = hitInfo.transform.gameObject;
		Assert.IsNotNull(hitObject, "Hit something but hitObject is null.");

		if (cellsManager.IsCell(hitObject)) {
			playerController.Place(cellsManager.CellPos(hitObject));
		}
	}
}
