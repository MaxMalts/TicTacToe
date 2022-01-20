using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Assertions;



public class RestartController : MonoBehaviour {

	GameStarter gameStarter;
	GameManager gameManager;

	PlayerInput playerInput;
	bool inputEnabled = false;
	int inputChangedFrame = -1;

	[SerializeField] GameObject restartText;


	public void OnTap(InputAction.CallbackContext context) {
		// The action that invoked this event is the tap action,
		// not the position action.

		if (context.phase == InputActionPhase.Performed && inputEnabled &&
			inputChangedFrame < Time.frameCount) {

			OnPointerRelease();
		}
	}

	void Awake() {
		playerInput = GetComponent<PlayerInput>();
		Assert.IsNotNull(playerInput, "No Player Input Component on Player.");

		if (restartText != null) {
			if (restartText.activeSelf) {
				Debug.LogWarning("Restart Text GameObject was active by default. " +
					"To work correctly, please disable it before the game starts.");
			}

		} else {
			Debug.LogWarning("restartLabel not set in inspector.");
		}
	}

	void Start() {
		gameManager = GameManager.Instance;
		Assert.IsNotNull(gameManager);

		gameStarter = GameStarter.Instance;
		Assert.IsNotNull(gameStarter);

		gameManager.GameFinished.AddListener(OnGameFinished);
	}

	void EnableInput() {
		if (!inputEnabled) {
			inputEnabled = true;
			inputChangedFrame = Time.frameCount;
		}
	}

	void DisableInput() {
		if (inputEnabled) {
			inputEnabled = false;
			inputChangedFrame = Time.frameCount;
		}
	}

	void OnGameFinished() {
		if (restartText != null) {
			restartText.SetActive(true);
		}
		EnableInput();
	}

	void OnPointerRelease() {
		DisableInput();
		if (restartText != null) {
			restartText.SetActive(false);
		}

		gameStarter.RestartGame();
	}
}
