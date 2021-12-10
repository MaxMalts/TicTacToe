using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



/// <summary>
/// Manages sign swap between games and starts or restarts the game.
/// </summary>
public class GameStarter : Unique<GameStarter> {

	GameManager gameManager;

	CellSign localSign;

	[SerializeField] GameObject restartLabel;


	public void StartNewGame() {
		Assert.IsNotNull(gameManager);
		Assert.IsTrue(localSign == CellSign.Cross || localSign == CellSign.Nought);

		if (restartLabel != null) {
			restartLabel.SetActive(false);
		}

		localSign = localSign == CellSign.Cross ? CellSign.Nought : CellSign.Cross;
		gameManager.StartGame(localSign);
	}

	void Awake() {
		object localSignObj;
		if (!SceneArgsManager.CurSceneArgs.TryGetValue("cell-sign", out localSignObj)) {
			Debug.LogWarning("No cell-sign passed to current scene. Using cross sign by default.");
			localSignObj = CellSign.Cross;
		}
		localSign = CellSign.Empty;
		try {
			localSign = (CellSign)localSignObj;
		} catch (InvalidCastException innerException) {
			throw new ArgumentException("Bad value passed to scene.", "cell-sign", innerException);
		}
		if (!Enum.IsDefined(typeof(CellSign), localSign) || localSign == CellSign.Empty) {
			throw new ArgumentException("Bad value passed to scene.", "cell-sign");
		}
	}

	void Start() {
		gameManager = GameManager.Instance;
		Assert.IsNotNull(gameManager);

		gameManager.StartGame(localSign);
	}
}
