using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GameModeButtonController : MonoBehaviour {

	[SerializeField] GameMode gameMode;
	public GameMode CorrespGameMode {
		get {
			return gameMode;
		}
	}


	public void OnClick() {
		MainMenuAPI.Instance.SetGameMode(gameMode);
	}
}
