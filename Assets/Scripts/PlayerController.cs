using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;



public class PlayerController : MonoBehaviour
{
	public enum PlayerType
	{
		local,
		remote
	}

	public struct PlaceContext
	{
		public PlayerType TypeOfPlayer { get; }
		public CellSign Sign { get; }
		public Vector2Int FieldPosition { get; }
		public GameObject Player { get; }

		public PlaceContext(
			PlayerType typeOfPlayer,
			CellSign sign,
			Vector2Int fieldPosition,
			GameObject player)
		{
			TypeOfPlayer = typeOfPlayer;
			Sign = sign;
			FieldPosition = fieldPosition;
			Player = player;
		}
	}

	public class PlacedEvent : UnityEvent<PlaceContext> { }

	public PlacedEvent PlacedListeners = new PlacedEvent();


	public PlayerType Type { get; private set; }
	public CellSign Sign { get; private set; }

	[SerializeField] private CellsManager cellsManager;



	public void SetSign(CellSign newSign)
	{
		Assert.IsTrue(Enum.IsDefined(typeof(CellSign), newSign) && newSign != CellSign.empty,
			"Bad newSign value.");

		Sign = newSign;
	}


	public void Place(Vector2Int pos)
	{
		cellsManager.SetCellSign(pos, Sign);

		PlaceContext context = new PlaceContext(Type, Sign, pos, gameObject);

		PlacedListeners.Invoke(context);
	}
}
