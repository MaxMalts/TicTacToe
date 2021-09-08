using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;



public class MainMenuAPI : MonoBehaviour {

	static MainMenuAPI instance;
	public static MainMenuAPI Instance {
		get {
			Assert.IsNotNull<MainMenuAPI>(instance, "No instance of MainMenuAPI.");
			return instance;
		}

		private set {
			instance = value;
		}
	}


	public void StartGame(CellSign sign) {
		MainMenu.Instance.ConnectAndStartGame(sign);
	}

	void OnEnable() {
		Assert.IsNull<MainMenuAPI>(instance, "You've enabled multiple MainMenyAPIs.");
		Instance = this;
	}

	void OnDisable() {
		Instance = null;
	}
}
