using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



/// <summary>
/// Manages sign swap between games and starts or restarts the game.
/// </summary>
public class GameStarter : Unique<GameStarter> {

	const float easyProb = 0.7f;  // probability of wrong cell in easy mode  
	const float hardProb = 0.0f;  // probability of wrong cell in hard mode

	[SerializeField] GameObject userPlayerPrefab;
	[SerializeField] GameObject aiPlayerPrefab;
	[SerializeField] GameObject remotePlayerPrefab;

	[SerializeField] GameObject restartLabel;


	public void RestartGame() {
		Assert.IsNotNull(GameManager.Instance);

		if (restartLabel != null) {
			restartLabel.SetActive(false);
		}

		PlayerAPI player1 = GameManager.Instance.Player1.PlayerApi;
		PlayerAPI player2 = GameManager.Instance.Player2.PlayerApi;
		Assert.IsNotNull(player1);
		Assert.IsNotNull(player2);

		CellSign t = player1.Sign;
		player1.Sign = player2.Sign;
		player2.Sign = t;

		GameManager.Instance.StartNewGame();
	}

	void Awake() {
		Assert.IsNotNull(userPlayerPrefab, "userPlayerPrefab was not assigned in inspector.");
		//Assert.IsNotNull(aiPlayerPrefab, "aiPlayerPrefab was not assigned in inspector.");
		Assert.IsNotNull(remotePlayerPrefab, "remotePlayerPrefab was not assigned in inspector.");

		GameMode gameMode;
		if (!SceneArgsManager.TryGetArg("game-mode", out gameMode)) {
			Debug.LogWarning("No or wrong type of game-mode passed to current scene. " +
				"Using Singleplayer by default.");
			gameMode = GameMode.Singleplayer;
		}

		switch (gameMode) {
			case GameMode.Singleplayer:
				InitSingleplayer();
				break;

			case GameMode.Multiplayer:
				InitMultiplayer();
				break;

			default:
				throw new ArgumentException("Bad game-mode value passed to scene", "game-mode");
		}
	}

	void Start() {
		Assert.IsNotNull(GameManager.Instance);
		GameManager.Instance.StartNewGame();
	}

	void InitSingleplayer() {
		throw new NotImplementedException();
	}

	void InitMultiplayer() {
		CellSign localSign;
		if (!SceneArgsManager.TryGetArg("local-cell-sign", out localSign)) {
			Debug.LogWarning("No or wrong type of local-cell-sign passed to current scene. " +
				"Using cross sign by default.");
			localSign = CellSign.Cross;
		}
		if (localSign != CellSign.Cross && localSign != CellSign.Nought) {
			throw new ArgumentException("Bad local-cell-sign value passed to scene.", "local-cell-sign");
		}

		PlayerController localPlayer = Instantiate(userPlayerPrefab).GetComponent<PlayerController>();
		Assert.IsNotNull(localPlayer, "No PlayerController component on userPlayerPrefab.");

		PlayerController remotePlayer = Instantiate(remotePlayerPrefab).GetComponent<PlayerController>();
		Assert.IsNotNull(remotePlayer, "No PlayerController component on remotePlayerPrefab.");

		localPlayer.PlayerApi.Sign = localSign;
		remotePlayer.PlayerApi.Sign = localSign == CellSign.Cross ? CellSign.Nought : CellSign.Cross;

		GameManager.Instance.Player1 = localPlayer;
		GameManager.Instance.Player2 = remotePlayer;
	}
}
