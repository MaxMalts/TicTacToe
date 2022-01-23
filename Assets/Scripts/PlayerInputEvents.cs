using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Assertions;



public class InputActionEvent : UnityEvent<InputAction.CallbackContext> { }


[RequireComponent(typeof(PlayerInput))]
public class PlayerInputEvents : Unique<PlayerInputEvents> {

	PlayerInput playerInput;
	public PlayerInput PlayerInputComponent {
		get {
			if (playerInput == null) {
				playerInput = GetComponent<PlayerInput>();
				Assert.IsNotNull(playerInput,
					"No PlayerInput component on PlayerInputEvents gameobject.");
			}

			return playerInput;
		}
	}

	public InputActionEvent TapEvent { get; private set; } = new InputActionEvent();
	public InputActionEvent MoveEvent { get; private set; } = new InputActionEvent();
	public InputActionEvent BackEvent { get; private set; } = new InputActionEvent();


	public void OnTap(InputAction.CallbackContext context) {
		TapEvent.Invoke(context);
	}

	public void OnMove(InputAction.CallbackContext context) {
		MoveEvent.Invoke(context);
	}

	public void OnBack(InputAction.CallbackContext context) {
		BackEvent.Invoke(context);
	}
}
