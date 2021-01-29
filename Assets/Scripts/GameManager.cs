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

	[SerializeField] private GameObject opponentPlayer;
	public GameObject OpponentPlayer {
		get {
			return opponentPlayer;
		}
	}

	private PlayerAPI localPlayerAPI;
	private LocalPlayerController localPlayerController;
	private PlayerAPI opponentPlayrAPI;

	public GameObject CurrentPlayer { get; private set; }



	public void Awake() {

		localPlayerAPI = localPlayer.GetComponent<PlayerAPI>();
		Assert.IsNotNull(localPlayerAPI, "No PlayerAPI on local player object.");

		localPlayerController = localPlayer.GetComponent<LocalPlayerController>();
		Assert.IsNotNull(localPlayerController, "No LocalPlayerController on local player object.");

		localPlayerAPI.Sign = CellSign.cross;
		localPlayerAPI.Type = PlayerAPI.PlayerType.local;
		localPlayerAPI.PlacedListeners.AddListener(OnCellPlaced);
	}


	public void Start() {
		CurrentPlayer = localPlayer;
	}


	private void OnCellPlaced(PlayerAPI.PlaceContext context) {

		if (context.PlayerType == PlayerAPI.PlayerType.local) {
			//localPlayerController.DisableInput();
		}

		if (CheckWin(context.Sign)) {
			HandleCurrentWin();
		} else {
			if (CheckDraw()) {
				HandleDraw();
			} else {
				//SwitchTurn();
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
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, opponentPlayer),
				"CurrentPlayer not valid.");

			Debug.Log("Local player lost.");
		}
	}


	private void SwitchTurn() {

		if (ReferenceEquals(CurrentPlayer, localPlayer)) {
			localPlayerController.DisableInput();
			CurrentPlayer = opponentPlayer;


		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, opponentPlayer),
				"CurrentPlayer not valid.");

			CurrentPlayer = localPlayer;
			localPlayerController.EnableInput();
		}
	}
}
