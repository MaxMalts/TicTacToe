using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;



public class PlayerAPI : MonoBehaviour {

	public enum PlayerType {
		Local,
		Remote
	}

	public struct PlaceContext {

		public PlayerType PlayerType { get; }
		public CellSign Sign { get; }
		public Vector2Int FieldPosition { get; }
		public PlayerAPI Player { get; }


		public PlaceContext(
			PlayerType typeOfPlayer,
			CellSign sign,
			Vector2Int fieldPosition,
			PlayerAPI player) {

			PlayerType = typeOfPlayer;
			Sign = sign;
			FieldPosition = fieldPosition;
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

			sign = value;
		}	
	}

	[SerializeField] CellsManager cellsManager;


	public void Place(Vector2Int pos) {

		cellsManager.SetCellSign(pos, Sign);
		cellsManager.DisableCellInput(pos);

		PlaceContext context = new PlaceContext(Type, Sign, pos, this);
		CellPlaced.Invoke(context);
	}
}
