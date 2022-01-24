using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



public class AIPlayerController : MonoBehaviour, PlayerController {

	public PlayerAPI PlayerApi { get; private set; }

	public bool InputEnabled { get; private set; }

	/// <summary>
	/// Probability of placing in a random cell.
	/// </summary>
	public float RandomCellProb { get; set; } = 0;

	const int fieldSize = GameManager.fieldSize;

	//Has values 'X' - cross, 'O' - nought or '.' - empty.
	//We are assuming that our sign is always 'X', even if it's actually nought.
	char[][] field;

	int inputEnabledFrame;  // to be sure OnCellPlaced was called

	bool firstMove = false;  // to not think on the first move


	public void StarNewGame() {
		Assert.IsTrue(PlayerApi.Sign == CellSign.Cross || PlayerApi.Sign == CellSign.Nought);

		Assert.IsTrue(ReferenceEquals(this, GameManager.Instance.Player1) ||
			ReferenceEquals(this, GameManager.Instance.Player2));

		PlayerController otherPlayer =
			ReferenceEquals(this, GameManager.Instance.Player1) ?
			GameManager.Instance.Player2 :
			GameManager.Instance.Player1;

		Assert.IsNotNull(otherPlayer);

		PlayerAPI otherPlayerApi = otherPlayer.PlayerApi;
		Assert.IsNotNull(otherPlayerApi);

		otherPlayerApi.CellPlaced.RemoveListener(OnOtherCellPlaced);
		otherPlayerApi.CellPlaced.AddListener(OnOtherCellPlaced);

		field = new char[fieldSize][];
		for (int i = 0; i < fieldSize; ++i) {
			field[i] = new char[fieldSize];
			for (int j = 0; j < fieldSize; ++j) {
				field[i][j] = '.';
			}
		}

		firstMove = true;
	}

	public void EnableInput() {
		InputEnabled = true;
		inputEnabledFrame = Time.frameCount;
	}

	public void DisableInput() {
		InputEnabled = false;
	}

	void Awake() {
		PlayerApi = GetComponent<PlayerAPI>();
		Assert.IsNotNull(PlayerApi, "No PlayerAPI script on AIPlayerController.");
		PlayerApi.Type = PlayerAPI.PlayerType.AI;
	}

	void Update() {
		if (InputEnabled && inputEnabledFrame < Time.frameCount) {
			Vector2Int? placePos = null;
			if (firstMove) {
				placePos =
					new Vector2Int(UnityEngine.Random.value < 0.5 ? 0 : fieldSize - 1,
					UnityEngine.Random.value < 0.5 ? 0 : fieldSize - 1);

				firstMove = false;

			} else if (UnityEngine.Random.value < RandomCellProb) {
				placePos = GetRandomEmptyCell();

			} else {
				placePos = Minimax('X').Item2;
			}
			Assert.IsTrue(placePos != null);

			PlayerApi.Place(new Vector2Int(placePos.Value.x + 1, placePos.Value.y + 1));
			field[placePos.Value.x][placePos.Value.y] = 'X';
		}
	}

	Vector2Int? GetRandomEmptyCell() {
		List<Vector2Int> emptyPoss = new List<Vector2Int>(fieldSize * fieldSize);
		for (int i = 0; i < fieldSize; ++i) {
			for (int j = 0; j < fieldSize; ++j) {
				if (field[i][j] == '.') {
					emptyPoss.Add(new Vector2Int(i, j));
				}
			}
		}

		if (emptyPoss.Count == 0) {
			return null;
		}

		return emptyPoss[UnityEngine.Random.Range(0, emptyPoss.Count)];
	}

	void OnOtherCellPlaced(PlayerAPI.PlaceContext context) {
		field[context.FieldPos.x - 1][context.FieldPos.y - 1] = 'O';
	}

	// This implementation is different from the one in GameManager.
	// Here you must collect filedSize cells in a row, whereas in GameManager
	// you can set the required number of cells in a row.
	// If implementing a larger than 3x3 field, reimplement this method!
	(bool, int) CheckEndState(Vector2Int lastCellPos) {
		Assert.IsTrue(lastCellPos.x >= 0 && lastCellPos.x < fieldSize);
		Assert.IsTrue(lastCellPos.y >= 0 && lastCellPos.y < fieldSize);

		char lastSign = field[lastCellPos.x][lastCellPos.y];
		Assert.IsTrue(lastSign == 'X' || lastSign == 'O');

		int winScore = lastSign == 'X' ? 1 : -1;

		int curX, curY;
		bool won = true;

		// left-right
		won = true;
		for (curX = 0, curY = lastCellPos.y; curX < fieldSize; ++curX) {
			if (field[curX][curY] != lastSign) {
				won = false;
				break;
			}
		}
		if (won) {
			return (true, winScore);
		}

		// bottom-top
		won = true;
		for (curX = lastCellPos.x, curY = 0; curY < fieldSize; ++curY) {
			if (field[curX][curY] != lastSign) {
				won = false;
				break;
			}
		}
		if (won) {
			return (true, winScore);
		}

		// leftbottom-righttop
		won = true;
		for (curX = 0, curY = 0; curX < fieldSize; ++curY, ++curX) {
			if (field[curX][curY] != lastSign) {
				won = false;
				break;
			}
		}
		if (won) {
			return (true, winScore);
		}

		// lefttop-rightbottom
		won = true;
		for (curX = 0, curY = fieldSize - 1; curX < fieldSize; --curY, ++curX) {
			if (field[curX][curY] != lastSign) {
				won = false;
				break;
			}
		}
		if (won) {
			return (true, winScore);
		}

		// draw check
		foreach (char[] curColumn in field) {
			foreach (char curCell in curColumn) {
				if (curCell == '.') {
					return (false, 0);
				}
			}
		}

		// is draw
		return (true, 0);
	}

	(int, Vector2Int?) Minimax(char curSign) {
		Assert.IsTrue(curSign == 'X' || curSign == 'O', "Bad curSign value.");

		Func<int, int, bool> comparator;
		if (curSign == 'X') {
			comparator = (int x, int y) => x > y;
		} else {
			Assert.IsTrue(curSign == 'O');
			comparator = (int x, int y) => x < y;
		}

		int? resScore = null;
		Vector2Int? resPos = null;
		for (int i = 0; i < fieldSize; ++i) {
			for (int j = 0; j < fieldSize; ++j) {
				if (field[i][j] == '.') {
					field[i][j] = curSign;

					(bool isEnd, int curScore) = CheckEndState(new Vector2Int(i, j));

					if (!isEnd) {
						curScore = Minimax(curSign == 'X' ? 'O' : 'X').Item1;
					}

					if (resScore == null || comparator(curScore, resScore.Value)) {
						resScore = curScore;
						resPos = new Vector2Int(i, j);
					}

					field[i][j] = '.';
				}
			}
		}

		Assert.IsTrue(resScore != null);
		return (resScore.Value, resPos);
	}
}
