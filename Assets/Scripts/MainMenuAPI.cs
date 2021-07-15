using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
		SceneArgsManager.NextSceneArgs.Add("cell-sign", sign);
		SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
	}


	void OnEnable() {
		Assert.IsNull<MainMenuAPI>(instance, "You've enabled multiple MainMenyAPI's.");
		Instance = this;
	}


	void OnDisable() {
		Instance = null;
	}
}
