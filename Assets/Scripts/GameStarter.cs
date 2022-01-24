using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



/// <summary>
/// Manages sign swap between games and starts or restarts the game.
/// </summary>
public class GameStarter : Unique<GameStarter> {

	// probabilities of ai placing random cell in different game modes
	const float easyProb = 1.0f;
	const float normalProb = 0.25f;
	const float hardcoreProb = 0.0f;

	[SerializeField] GameObject userPlayerPrefab;
	[SerializeField] GameObject aiPlayerPrefab;
	[SerializeField] GameObject remotePlayerPrefab;


	public void RestartGame() {
		Assert.IsNotNull(GameManager.Instance);

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
		Assert.IsNotNull(aiPlayerPrefab, "aiPlayerPrefab was not assigned in inspector.");
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
		CellSign userSign = UnityEngine.Random.value > 0.5f ? CellSign.Cross : CellSign.Nought;

		PlayerController userPlayer = Instantiate(userPlayerPrefab).GetComponent<PlayerController>();
		Assert.IsNotNull(userPlayer, "No PlayerController component on userPlayerPrefab.");

		AIPlayerController aiPlayer = Instantiate(aiPlayerPrefab).GetComponent<AIPlayerController>();
		Assert.IsNotNull(aiPlayer, "No AIPlayerController component on aiPlayerPrefab.");

		userPlayer.PlayerApi.Sign = userSign;
		aiPlayer.PlayerApi.Sign = userSign == CellSign.Cross ? CellSign.Nought : CellSign.Cross;

		GameDifficulty difficulty;
		if (!SceneArgsManager.TryGetArg("game-difficulty", out difficulty)) {
			Debug.LogWarning("No or wrong type of game-difficulty passed to current scene. " +
				"Using normal difficulty by default.");
			difficulty = GameDifficulty.Normal;
		}

		switch(difficulty) {
			case GameDifficulty.Easy:
				aiPlayer.RandomCellProb = easyProb;
				break;

			case GameDifficulty.Normal:
				aiPlayer.RandomCellProb = normalProb;
				break;

			case GameDifficulty.Hardcore:
				aiPlayer.RandomCellProb = hardcoreProb;
				break;

			default:
				throw new ArgumentException("Bad game-difficulty value passed to scene",
					"game-difficulty");
		}

		GameManager.Instance.Player1 = userPlayer;
		GameManager.Instance.Player2 = aiPlayer;
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
