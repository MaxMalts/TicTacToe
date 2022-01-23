using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Assertions;



public class RestartController : MonoBehaviour {

	[SerializeField] GameObject restartText;

	GameStarter gameStarter;
	GameManager gameManager;

	InputActionEvent tapEvent;
	bool inputEnabled = false;
	//int inputChangedFrame = -1;


	public void OnTap(InputAction.CallbackContext context) {
		if (context.phase == InputActionPhase.Performed && inputEnabled) {// &&
			//inputChangedFrame < Time.frameCount) {
			Assert.IsTrue(inputEnabled);

			OnPointerRelease();
		}
	}

	void Awake() {
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
		if (tapEvent == null) {
			tapEvent = PlayerInputEvents.Instance?.TapEvent;
			Assert.IsNotNull(tapEvent);
		}

		if (!inputEnabled) {
			inputEnabled = true;
			tapEvent.AddListener(OnTap);
			//inputChangedFrame = Time.frameCount;
		}
	}

	void DisableInput() {
		if (inputEnabled) {
			inputEnabled = false;
			tapEvent.RemoveListener(OnTap);
			//inputChangedFrame = Time.frameCount;
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
