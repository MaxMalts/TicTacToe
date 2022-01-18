using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using System;



/// <summary>
/// Used for the outer world to interact with MainMenu. Also works closely with UI.
/// </summary>

public class MainMenuAPI : Unique<MainMenuAPI> {

	[SerializeField] GameObject gameModeScreen;
	[SerializeField] GameObject difficultyScreen;
	[SerializeField] GameObject signChoiceScreen;

	MainMenu.GameMode? curGameMode = null;


	public void SetGameMode(MainMenu.GameMode gameMode) {
		curGameMode = gameMode;
	}

	public void SetDifficultyAndStart(MainMenu.Difficulty difficulty) {
		Assert.IsTrue(curGameMode != null, "Game difficulty set but game mode not set.");
		Assert.IsTrue(curGameMode == MainMenu.GameMode.Singleplayer);

		MainMenu.Instance.StartGame(curGameMode.Value, difficulty);
	}

	public void SetSignAndStart(CellSign sign) {
		Assert.IsTrue(curGameMode != null, "Game difficulty set but game mode not set.");
		Assert.IsTrue(curGameMode == MainMenu.GameMode.Multiplayer);

		MainMenu.Instance.StartGame(curGameMode.Value, null, sign);
	}

	public void Back() {
		if (!MainMenu.Instance.Connecting) {
			if (curGameMode != null) {
				curGameMode = null;
				difficultyScreen.SetActive(false);
				signChoiceScreen.SetActive(false);
				gameModeScreen.SetActive(true);

			} else {
				MainMenu.Instance.Quit();
			}
		}
	}

	void Awake() {
		Assert.IsNotNull(gameModeScreen, "gameModeScreen was not assigned in inspector.");
		Assert.IsNotNull(difficultyScreen, "difficultyScreen was not assigned in inspector.");
		Assert.IsNotNull(signChoiceScreen, "signChoiceScreen was not assigned in inspector.");

		BackHandler.Instance.OnBack.AddListener(Back);
	}
}