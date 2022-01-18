using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class DifficultyButtonController : MonoBehaviour {

	[SerializeField] MainMenu.Difficulty difficulty;
	public MainMenu.Difficulty CorrespDifficulty {
		get {
			return difficulty;
		}
	}


	public void OnClick() {
		MainMenuAPI.Instance.SetDifficultyAndStart(difficulty);
	}
}
