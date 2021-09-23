using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.Assertions;



[RequireComponent(typeof(PlayerAPI))]
public class LocalPlayerController : MonoBehaviour, PlayerController {

	public PlayerAPI PlayerApi { get; private set; }

	public bool InputEnabled { get; private set; } = false;

	[SerializeField] CellsManager cellsManager;

	[SerializeField] new Camera camera;
	
	PlayerInput playerInput;
	[SerializeField] InputActionAsset actionsAsset;
	InputAction pointerPosAction;  // to get the position of tap



	public void StartGame(CellSign sign) {
		Assert.IsTrue(sign == CellSign.Cross || sign == CellSign.Nought);

		PlayerApi.Sign = sign;
		InputEnabled = false;
	}

	//public void Update() {
	//	// I wrote these lines and then realized that when you
	//	// have a Simulator tab open then the mouse is disabled by default

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

	public void EnableInput() {
		InputEnabled = true;
	}

	public void DisableInput() {
		InputEnabled = false;
	}

	public void OnTap(InputAction.CallbackContext context) {
		// The action that invoked this event is the tap action,
		// not the position action.

		if (context.phase == InputActionPhase.Performed && InputEnabled) {
			OnPointerRelease();
		}
	}

	void OnPointerRelease() {
#if INTERACTION_LOG
		Debug.Log("Pointer Released.");
#endif

		Assert.IsTrue(pointerPosAction.phase == InputActionPhase.Started);

		Vector2 pointerPos = pointerPosAction.ReadValue<Vector2>();
		Vector2 worldPos = camera.ScreenToWorldPoint(pointerPos);

		RaycastHit2D hitInfo = Physics2D.Raycast(worldPos, Vector2.zero, 0);
		if (hitInfo.collider == null) {
#if INTERACTION_LOG
			Debug.Log("Pointer release position was not on cell.");
#endif
			return;
		}
#if INTERACTION_LOG
		Debug.Log("Pointer release position was on cell.");
#endif

		GameObject hitObject = hitInfo.transform.gameObject;
		Assert.IsNotNull(hitObject, "Hit something but hitObject is null.");

		if (cellsManager.IsCell(hitObject)) {
			PlayerApi.Place(cellsManager.CellPos(hitObject));
		}
	}

	void Awake() {
		playerInput = GetComponent<PlayerInput>();
		Assert.IsNotNull(playerInput, "No Player Input Component on Player.");

		PlayerApi = GetComponent<PlayerAPI>();
		Assert.IsNotNull(PlayerApi, "No PlayerAPI script on PlayerApi.");
		PlayerApi.Type = PlayerAPI.PlayerType.Local;
	}

	void Start() {
		pointerPosAction = actionsAsset.FindAction("Screen Interactions/Position", true);
	}
}
