using UnityEngine;



public interface PlayerController {

	PlayerAPI PlayerApi { get; }

	bool InputEnabled { get; }

	void StartGame();

	void EnableInput();

	void DisableInput();
}