using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

// To do: merge this class with PlayerController and make it a base class for all players

/// <summary>
/// This classed is used for PlayerController to control player. But it can also be used
/// to get player information and events.
/// </summary>
public class PlayerAPI : MonoBehaviour {

	public enum PlayerType {
		User,
		Remote,
		AI
	}

	public struct PlaceContext {

		public PlayerType PlayerType { get; }
		public CellSign Sign { get; }
		public Vector2Int FieldPos { get; }
		public PlayerAPI Player { get; }


		public PlaceContext(
			PlayerType typeOfPlayer,
			CellSign sign,
			Vector2Int fieldPosition,
			PlayerAPI player) {

			PlayerType = typeOfPlayer;
			Sign = sign;
			FieldPos = fieldPosition;
			Player = player;
		}
	}

	public class PlacedEvent : UnityEvent<PlaceContext> { }
	public PlacedEvent CellPlaced { get; } = new PlacedEvent();

	PlayerType type;
	public PlayerType Type {
		get {
			return type;
		}

		set {
			Assert.IsTrue(Enum.IsDefined(typeof(PlayerType), value), "Bad PlayerType value.");

			type = value;
		}
	}

	CellSign sign;
	public CellSign Sign {
		get {
			return sign;
		}

		set {
			Assert.IsTrue(Enum.IsDefined(typeof(CellSign), value) && value != CellSign.Empty,
				"Bad newSign value.");

			if (!GameManager.Instance.GameRunning) {
				sign = value;
			} else {
				Debug.LogWarning("Trying to set player sign when game is running. Ignoring.", this);
			}
		}
	}

	CellsManager cellsManager;


	public void Place(Vector2Int pos) {
		cellsManager.SetCellSign(pos, Sign);
		cellsManager.DisableCellInput(pos);

		PlaceContext context = new PlaceContext(Type, Sign, pos, this);
		CellPlaced.Invoke(context);
	}

	void Start() {
		cellsManager = CellsManager.Instance;
		Assert.IsNotNull(cellsManager, "No CellsManager instance on PlayerAPI start.");
	}
}
