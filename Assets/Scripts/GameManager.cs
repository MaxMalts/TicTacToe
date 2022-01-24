using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using TMPro;



public class GameManager : Unique<GameManager> {

	public const int fieldSize = 3;

	public UnityEvent GameFinished { get; } = new UnityEvent();
	public UnityEvent ReturningToMainMenu { get; } = new UnityEvent();

	[SerializeField] CellsManager cellsManager;
	public CellsManager CellsManager {
		get {
			return cellsManager;
		}
	}

	PlayerController player1;
	public PlayerController Player1 {
		get {
			return player1;
		}

		set {
			if (GameRunning) {
				Debug.LogWarning("Trying to set player1 while game is running. Ignoring.", this);
			} else {
				player1 = value;
			}
		}
	}

	PlayerController player2;
	public PlayerController Player2 {
		get {
			return player2;
		}

		set {
			if (GameRunning) {
				Debug.LogWarning("Trying to set player2 while game is running. Ignoring.", this);
			} else {
				player2 = value;
			}
		}
	}

	public PlayerController CurrentPlayer { get; private set; }

	public bool GameRunning { get; private set; }

	// To do: custom text on different modes
	const string player1TurnStatus = "Your turn";
	const string player2TurnStatus = "Opponent's turn";
	const string player1WonStatus = "You won";
	const string player2WonStatus = "You lost";
	const string drawStatus = "Draw";

	[SerializeField] WinningLine winLine;
	[SerializeField] TextMeshProUGUI statusText;

	delegate bool WinIterationProcessor(ref Vector2Int? winPos1, ref Vector2Int? winPos2);


	public void StartNewGame() {
		Assert.IsTrue(player1 != null, "player1 not set on game start.");
		Assert.IsTrue(player2 != null, "player2 not set on game start.");
		Assert.IsTrue(
			(player1.PlayerApi.Sign == CellSign.Cross &&
			player2.PlayerApi.Sign == CellSign.Nought) ||
			(player1.PlayerApi.Sign == CellSign.Nought &&
			player2.PlayerApi.Sign == CellSign.Cross),
			"Players have not corresponding cell signs."
		);

		player1.PlayerApi.CellPlaced.RemoveListener(OnCellPlaced);
		player2.PlayerApi.CellPlaced.RemoveListener(OnCellPlaced);
		player1.PlayerApi.CellPlaced.AddListener(OnCellPlaced);
		player2.PlayerApi.CellPlaced.AddListener(OnCellPlaced);

		cellsManager.ResetAllCells();
		winLine.Hide();

		GameRunning = true;

		player1.StarNewGame();
		player2.StarNewGame();

		if (player1.PlayerApi.Sign == CellSign.Cross) {
			CurrentPlayer = player1;
			player1.EnableInput();
			statusText.text = player1TurnStatus;

		} else {
			Assert.IsTrue(player2.PlayerApi.Sign == CellSign.Cross);

			CurrentPlayer = player2;
			player2.EnableInput();
			statusText.text = player2TurnStatus;
		}
	}

	public void SuspendGame() {
		player1.DisableInput();
		player2.DisableInput();
	}

	public void UnsuspendGame() {
		if (ReferenceEquals(CurrentPlayer, player1)) {
			Assert.IsFalse(player2.InputEnabled, "Not current player has input enabled.");

			player1.EnableInput();

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, player2));
			Assert.IsFalse(player1.InputEnabled, "Not current player has input enabled.");

			player2.EnableInput();
		}
	}

	public void ReturnToMainMenu() {
		ReturningToMainMenu.Invoke();
		SceneManager.LoadScene((int)SceneIndeces.MainMenu);
	}

	void Awake() {
		Assert.IsNotNull(cellsManager, "Cells Manager was not assigned in inspector.");
		Assert.IsNotNull(winLine, "Winning Line was not assigned in inspector.");
		Assert.IsNotNull(statusText, "Status Text was not assigned in inspector.");
	}

	void OnCellPlaced(PlayerAPI.PlaceContext context) {
		//if (context.PlayerType == PlayerAPI.PlayerType.User) {
		//	player1.DisableInput();
		//}

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

	bool CheckCurrentWin(
		Vector2Int fieldPos,
		out Vector2Int? winPos1,
		out Vector2Int? winPos2
	) {

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
		if (ReferenceEquals(CurrentPlayer, player1)) {
			statusText.text = player1WonStatus;
			Debug.Log("Local player won.");

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, player2),
				"CurrentPlayer not valid.");

			statusText.text = player2WonStatus;
			Debug.Log("Local player lost.");
		}

		winLine.SetLine(fieldPos1, fieldPos2);
		winLine.Show();
	}

	void SwitchTurn() {
		if (ReferenceEquals(CurrentPlayer, player1)) {
			player1.DisableInput();
			CurrentPlayer = player2;
			statusText.text = player2TurnStatus;
			player2.EnableInput();

		} else {
			Assert.IsTrue(ReferenceEquals(CurrentPlayer, player2),
				"CurrentPlayer not valid.");

			player2.DisableInput();
			CurrentPlayer = player1;
			statusText.text = player1TurnStatus;
			player1.EnableInput();
		}
	}

	void HandleGameFinished() {
		Debug.Log("Game finished.");
		player1.DisableInput();
		player2.DisableInput();
		GameRunning = false;
		GameFinished.Invoke();
	}
}
