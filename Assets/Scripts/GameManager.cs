using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



public class GameManager : MonoBehaviour {

	public const int fieldSize = 3;

	[SerializeField] private CellsManager cellsManager;
	public CellsManager CellsManager {
		get {
			return cellsManager;
		}
	}

	[SerializeField] private GameObject localPlayer;
	public GameObject LocalPlayer {
		get {
			return localPlayer;
		}
	}

	[SerializeField] private GameObject remotePlayer;
	public GameObject RemotePlayer {
		get {
			return remotePlayer;
		}
	}

	private PlayerAPI localPlayerAPI;
	private LocalPlayerController localPlayerController;
	private PlayerAPI remotePlayerAPI;
	private RemotePlayerController remotePlayerController;

	public GameObject CurrentPlayer { get; private set; }


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
			remotePlayerAPI.Sign = CellSign.nought;
		}
		localPlayerAPI.PlacedListeners.AddListener(OnCellPlaced);
	}


	public void Start() {
		CurrentPlayer = localPlayer;
	}


	private void OnCellPlaced(PlayerAPI.PlaceContext context) {

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


	private bool CheckWin(CellSign sign) {
		return false;
	}


	private bool CheckDraw() {
		return false;
	}


	private void HandleDraw() {
		Debug.Log("Draw.");
	}


	private void HandleCurrentWin() {

		if (ReferenceEquals(CurrentPlayer, localPlayer)) {
			Debug.Log("Local player won.");

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, remotePlayer),
				"CurrentPlayer not valid.");

			Debug.Log("Local player lost.");
		}
	}


	private void SwitchTurn() {

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
