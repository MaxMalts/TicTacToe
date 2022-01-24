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

	[SerializeField] InputActionAsset actionsAsset;
	InputAction pointerPosAction;  // to get the position of tap

	new Camera camera;
	CellsManager cellsManager;
	InputActionEvent tapEvent;


	public void StarNewGame() {
		Assert.IsTrue(PlayerApi.Sign == CellSign.Cross || PlayerApi.Sign == CellSign.Nought);
		DisableInput();
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
		if (tapEvent == null) {
			tapEvent = PlayerInputEvents.Instance?.TapEvent;
			Assert.IsNotNull(tapEvent);
		}

		tapEvent.AddListener(OnTap);
		InputEnabled = true;
	}

	public void DisableInput() {
		if (tapEvent != null) {
			tapEvent.RemoveListener(OnTap);
		}

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

		GameObject hitObject = hitInfo.transform.gameObject;
		Assert.IsNotNull(hitObject, "Hit something but hitObject is null.");

		if (cellsManager.IsCell(hitObject)) {
#if INTERACTION_LOG
			Debug.Log("Pointer release position was on cell.");
# endif
			PlayerApi.Place(cellsManager.CellPos(hitObject));
		}
	}

	void Awake() {
		Assert.IsNotNull(actionsAsset, "actionAsset was not assigned in inspector");

		camera = Camera.main;

		PlayerApi = GetComponent<PlayerAPI>();
		Assert.IsNotNull(PlayerApi, "No PlayerAPI script on LocalPlayerController.");
		PlayerApi.Type = PlayerAPI.PlayerType.User;
	}

	void Start() {
		pointerPosAction = actionsAsset.FindAction("Screen Interactions/Position", true);

		cellsManager = CellsManager.Instance;
		Assert.IsNotNull(cellsManager, "No CellsManager instance on LocalPlayerController start.");
	}
}