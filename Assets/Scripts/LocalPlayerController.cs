using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Assertions;



public class LocalPlayerController : MonoBehaviour, PlayerController {

	public PlayerAPI PlayerApi { get; private set; }

	[SerializeField] private new Camera camera;
	private PlayerInput playerInput;

	public CellsManager cellsManager;

	[SerializeField] private InputActionAsset actionsAsset;
	private InputAction pointerPosAction;


	public void Awake() {
		playerInput = GetComponent<PlayerInput>();
		Assert.IsNotNull(playerInput, "No Player Input Component on Player.");

		PlayerApi = GetComponent<PlayerAPI>();
		Assert.IsNotNull(PlayerApi, "No PlayerAPI script on PlayerApi.");
		PlayerApi.Type = PlayerAPI.PlayerType.Local;

		playerInput.enabled = false;
	}


	public void Start() {
		pointerPosAction = actionsAsset.FindAction("Screen Interactions/Position", true);
	}


	//public void Update() {
	//	// I wrote these lines and then realized that when you
	//	// have a Simulator tab open then the muse is disabled by default

	//	//if (Mouse.current != null) {
	//	//	InputSystem.EnableDevice(Mouse.current);
	//	//}
	//	//if (Pointer.current != null) {
	//	//	InputSystem.EnableDevice(Pointer.current);
	//	//}
	//	//if (Touchscreen.current != null) {
	//	//	InputSystem.EnableDevice(Touchscreen.current);
	//	//}
	//}


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

#if INTERACTION_LOG
		Debug.Log("Pointer Released: " + context.ToString());
#endif

		Vector2 pointerPos = pointerPosAction.ReadValue<Vector2>();
		Vector2 worldPos = camera.ScreenToWorldPoint(pointerPos);

		RaycastHit2D hitInfo = Physics2D.Raycast(worldPos, Vector2.zero, 0);
		if (hitInfo.collider == null) {
#if INTERACTION_LOG
			Debug.Log("Pointer release position was not on colider.");
#endif
			return;
		}
#if INTERACTION_LOG
		Debug.Log("Pointer release position was on colider.");
#endif

		GameObject hitObject = hitInfo.transform.gameObject;
		Assert.IsNotNull(hitObject, "Hit something but hitObject is null.");

		if (cellsManager.IsCell(hitObject)) {
			PlayerApi.Place(cellsManager.CellPos(hitObject));
		}
	}
}
