using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



public class GameManager : MonoBehaviour {

	public const int fieldSize = 3;

	[SerializeField] CellsManager cellsManager;
	public CellsManager CellsManager {
		get {
			return cellsManager;
		}
	}

	[SerializeField] GameObject localPlayer;
	public GameObject LocalPlayer {
		get {
			return localPlayer;
		}
	}

	[SerializeField] GameObject remotePlayer;
	public GameObject RemotePlayer {
		get {
			return remotePlayer;
		}
	}

	public GameObject CurrentPlayer { get; private set; }

	PlayerAPI localPlayerAPI;
	LocalPlayerController localPlayerController;
	PlayerAPI remotePlayerAPI;
	RemotePlayerController remotePlayerController;


	public void Awake() {
		Assert.IsNotNull(localPlayer, "Local Player was not assigned in inspector.");
		Assert.IsNotNull(remotePlayer, "Remote Player was not assigned in inspector.");

		localPlayerAPI = localPlayer.GetComponent<PlayerAPI>();
		Assert.IsNotNull(localPlayerAPI, "No PlayerAPI on Local player object.");

		localPlayerController = localPlayer.GetComponent<LocalPlayerController>();
		Assert.IsNotNull(localPlayerController, "No LocalPlayerController on local player object.");
		
		remotePlayerAPI = remotePlayer.GetComponent<PlayerAPI>();
		Assert.IsNotNull(remotePlayerAPI, "No PlayerAPI on remote player object.");

		remotePlayerController = remotePlayer.GetComponent<RemotePlayerController>();
		Assert.IsNotNull(remotePlayerController, "No RemotePlayerController on remote player object.");

		object localSignObj;
		if (!SceneArgsManager.CurSceneArgs.TryGetValue("cell-sign", out localSignObj)) {
			Debug.LogWarning("No cell-sign passed to current scene. Using cross sign by default.");
			localSignObj = CellSign.Cross;
		}
		CellSign localSign = CellSign.Empty;
		try {
			localSign = (CellSign)localSignObj;
		} catch (InvalidCastException innerException) {
			throw new ArgumentException("Bad value passed to scene.", "cell-sign", innerException);
		}
		if (!Enum.IsDefined(typeof(CellSign), localSign) || localSign == CellSign.Empty) {
			throw new ArgumentException("Bad value passed to scene.", "cell-sign");
		}

		if (localSign == CellSign.Cross) {
			localPlayerAPI.Sign = CellSign.Cross;
			remotePlayerAPI.Sign = CellSign.Nought;
		} else {
			localPlayerAPI.Sign = CellSign.Nought;
			remotePlayerAPI.Sign = CellSign.Cross;
		}

		localPlayerAPI.CellPlaced.AddListener(OnCellPlaced);
	}


	public void Start() {
		if (localPlayerAPI.Sign == CellSign.Cross) {
			CurrentPlayer = localPlayer;
			localPlayerController.EnableInput();

		} else {
			Assert.IsTrue(remotePlayerAPI.Sign == CellSign.Cross, "No one has cross sign.");

			CurrentPlayer = remotePlayer;
			remotePlayerController.EnableInput();
		}
	}


	void OnCellPlaced(PlayerAPI.PlaceContext context) {

		if (context.PlayerType == PlayerAPI.PlayerType.Local) {
			localPlayerController.DisableInput();
		}

		if (CheckWin(context.Sign)) {
			HandleCurrentWin();
		} else {
			if (CheckDraw()) {
				HandleDraw();
			} else {
				SwitchTurn();
			}
		}
	}


	bool CheckWin(CellSign sign) {
		return false;
	}


	bool CheckDraw() {
		return false;
	}


	void HandleDraw() {
		Debug.Log("Draw.");
	}


	void HandleCurrentWin() {

		if (ReferenceEquals(CurrentPlayer, localPlayer)) {
			Debug.Log("Local player won.");

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, remotePlayer),
				"CurrentPlayer not valid.");

			Debug.Log("Local player lost.");
		}
	}


	void SwitchTurn() {

		if (ReferenceEquals(CurrentPlayer, localPlayer)) {
			localPlayerController.DisableInput();
			CurrentPlayer = remotePlayer;

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, remotePlayer),
				"CurrentPlayer not valid.");

			CurrentPlayer = localPlayer;
			localPlayerController.EnableInput();
		}
	}
}
