using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class GameModeButtonController : MonoBehaviour {

	[SerializeField] MainMenu.GameMode gameMode;
	public MainMenu.GameMode CorrespGameMode {
		get {
			return gameMode;
		}
	}


	public void OnClick() {
		MainMenuAPI.Instance.SetGameMode(gameMode);
	}
}
