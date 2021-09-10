using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;



public class MainMenuAPI : Unique<MainMenuAPI> {

	public void StartGame(CellSign sign) {
		MainMenu.Instance.ConnectAndStartGame(sign);
	}
}
