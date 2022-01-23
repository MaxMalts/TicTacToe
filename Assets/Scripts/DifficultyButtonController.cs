using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class DifficultyButtonController : MonoBehaviour {

	[SerializeField] GameDifficulty difficulty;
	public GameDifficulty CorrespDifficulty {
		get {
			return difficulty;
		}
	}


	public void OnClick() {
		MainMenuAPI.Instance.SetDifficultyAndStart(difficulty);
	}
}
