using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using TMPro;



public class GameManager : Unique<GameManager> {

	public const int fieldSize = 3;

	public UnityEvent GameFinished { get; private set; }

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

	const string yourTurnStatus = "Your turn";
	const string opponentTurnStatus = "Opponent's turn";
	const string youWonStatus = "You won";
	const string youLostStatus = "You lost";
	const string drawStatus = "Draw";

	PlayerAPI localPlayerAPI;
	LocalPlayerController localPlayerController;
	PlayerAPI remotePlayerAPI;
	RemotePlayerController remotePlayerController;

	[SerializeField] WinningLine winLine;
	[SerializeField] TextMeshProUGUI statusText;

	delegate bool WinIterationProcessor(ref Vector2Int? winPos1, ref Vector2Int? winPos2);


	public void StartGame(CellSign localSign) {
		Assert.IsTrue(localSign == CellSign.Cross ||
			localSign == CellSign.Nought);

		cellsManager.ResetAllCells();
		winLine.Hide();

		CellSign remoteSign =
			localSign == CellSign.Cross ? CellSign.Nought : CellSign.Cross;

		if (localSign == CellSign.Cross) {
			CurrentPlayer = localPlayer;
			localPlayerController.EnableInput();
			statusText.text = yourTurnStatus;

		} else {
			CurrentPlayer = remotePlayer;
			remotePlayerController.EnableInput();
			statusText.text = opponentTurnStatus;
		}

		localPlayerController.StartGame(localSign);
		remotePlayerController.StartGame(remoteSign);
	}

	public void SuspendGame() {
		localPlayerController.DisableInput();
		remotePlayerController.DisableInput();
	}

	public void UnsuspendGame() {
		if (ReferenceEquals(CurrentPlayer, localPlayer)) {
			Assert.IsFalse(remotePlayerController.InputEnabled,
				"Not current player has input enabled.");

			localPlayerController.EnableInput();

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, remotePlayer));
			Assert.IsFalse(localPlayerController.InputEnabled,
				"Not current player has input enabled.");

			remotePlayerController.EnableInput();
		}
	}

	void Awake() {
		Assert.IsNotNull(cellsManager, "Cells Manager was not assigned in inspector.");
		Assert.IsNotNull(localPlayer, "Local Player was not assigned in inspector.");
		Assert.IsNotNull(remotePlayer, "Remote Player was not assigned in inspector.");
		Assert.IsNotNull(winLine, "Winning Line was not assigned in inspector.");
		Assert.IsNotNull(statusText, "Status Text was not assigned in inspector.");

		GameFinished = new UnityEvent();

		localPlayerAPI = localPlayer.GetComponent<PlayerAPI>();
		Assert.IsNotNull(localPlayerAPI, "No PlayerAPI on LocalPlayer object.");

		localPlayerController = localPlayer.GetComponent<LocalPlayerController>();
		Assert.IsNotNull(localPlayerController, "No LocalPlayerController on Local Player object.");

		remotePlayerAPI = remotePlayer.GetComponent<PlayerAPI>();
		Assert.IsNotNull(remotePlayerAPI, "No PlayerAPI on RemotePlayer object.");

		remotePlayerController = remotePlayer.GetComponent<RemotePlayerController>();
		Assert.IsNotNull(remotePlayerController, "No RemotePlayerController on Remote Player object.");

		localPlayerAPI.CellPlaced.AddListener(OnCellPlaced);
		remotePlayerAPI.CellPlaced.AddListener(OnCellPlaced);
	}

	void OnCellPlaced(PlayerAPI.PlaceContext context) {

		if (context.PlayerType == PlayerAPI.PlayerType.Local) {
			localPlayerController.DisableInput();
		}

		Vector2Int? winPos1, winPos2;
		if (CheckCurrentWin(context.FieldPos, out winPos1, out winPos2)) {
			Assert.IsTrue(winPos1.HasValue && winPos2.HasValue,
				"Winning configuration detected but positions not set.");

			HandleCurrentWin(winPos1.Value, winPos2.Value);
			HandleGameFinished();

		} else {
			if (CheckDraw()) {
				HandleDraw();
				HandleGameFinished();
			} else {
				SwitchTurn();
			}
		}
	}

	bool CheckCurrentWin(Vector2Int fieldPos,
		out Vector2Int? winPos1,
		out Vector2Int? winPos2) {

		const int winNumInRow = 3;

		CellSign curTurnCell = cellsManager.GetCellSign(fieldPos);

		int curInRowCount = 0;
		winPos1 = winPos2 = null;
		Vector2Int curPos = Vector2Int.zero;

		WinIterationProcessor processCurIteration =
			(ref Vector2Int? pos1, ref Vector2Int? pos2) => {
				if (!cellsManager.FieldPosInRange(curPos)) {
					return false;
				}

				if (cellsManager.GetCellSign(curPos) == curTurnCell) {
					++curInRowCount;
					Assert.IsTrue(curInRowCount <= winNumInRow,
						"curInRowCount exceeded the number to win in row.");

					if (curInRowCount == 1) {
						Assert.IsTrue(pos1 == null,
							"Detected the first cell in a row but first position is already set.");
						pos1 = curPos;
					}

					if (curInRowCount == winNumInRow) {
						Assert.IsTrue(pos1 != null,
							"Found winning configuration but first position of it is null.");
						Assert.IsTrue(pos2 == null,
							"Found winning configuration but second position is already set.");

						pos2 = curPos;
						return true;
					}

				} else {
					curInRowCount = 0;
					pos1 = null;
				}

				return false;
			};

		// leftup-rightdown
		curInRowCount = 0;
		winPos1 = winPos2 = null;
		curPos = new Vector2Int(fieldPos.x - winNumInRow + 1,
			fieldPos.y + winNumInRow - 1);
		for (int i = 0; i < winNumInRow + 2; ++i) {
			if (processCurIteration(ref winPos1, ref winPos2)) {
				return true;
			}

			curPos.x += 1;
			curPos.y -= 1;
		}


		// left-right
		curInRowCount = 0;
		winPos1 = winPos2 = null;
		curPos = new Vector2Int(fieldPos.x - winNumInRow + 1, fieldPos.y);
		for (int i = 0; i < winNumInRow + 2; ++i) {
			if (processCurIteration(ref winPos1, ref winPos2)) {
				return true;
			}

			curPos.x += 1;
		}


		// leftdown-rightup
		curInRowCount = 0;
		winPos1 = winPos2 = null;
		curPos = new Vector2Int(fieldPos.x - winNumInRow + 1,
			fieldPos.y - winNumInRow + 1);
		for (int i = 0; i < winNumInRow + 2; ++i) {
			if (processCurIteration(ref winPos1, ref winPos2)) {
				return true;
			}

			curPos.x += 1;
			curPos.y += 1;
		}

		// down-up
		curInRowCount = 0;
		winPos1 = winPos2 = null;
		curPos = new Vector2Int(fieldPos.x, fieldPos.y - winNumInRow + 1);
		for (int i = 0; i < winNumInRow + 2; ++i) {
			if (processCurIteration(ref winPos1, ref winPos2)) {
				return true;
			}

			curPos.y += 1;
		}

		return false;
	}

	bool CheckDraw() {
		return cellsManager.CalcCellsBySign(CellSign.Empty) == 0;
	}

	void HandleDraw() {
		statusText.text = drawStatus;
		Debug.Log("Draw.");
	}

	void HandleCurrentWin(Vector2Int fieldPos1, Vector2Int fieldPos2) {
		if (ReferenceEquals(CurrentPlayer, localPlayer)) {
			statusText.text = youWonStatus;
			Debug.Log("Local player won.");

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, remotePlayer),
				"CurrentPlayer not valid.");

			statusText.text = youLostStatus;
			Debug.Log("Local player lost.");
		}

		winLine.SetLine(fieldPos1, fieldPos2);
		winLine.Show();
	}

	void SwitchTurn() {
		if (ReferenceEquals(CurrentPlayer, localPlayer)) {
			localPlayerController.DisableInput();
			CurrentPlayer = remotePlayer;
			statusText.text = opponentTurnStatus;
			remotePlayerController.EnableInput();

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, remotePlayer),
				"CurrentPlayer not valid.");

			remotePlayerController.DisableInput();
			CurrentPlayer = localPlayer;
			statusText.text = yourTurnStatus;
			localPlayerController.EnableInput();
		}
	}

	void HandleGameFinished() {
		Debug.Log("Game finished.");
		GameFinished.Invoke();
	}
}
