
public interface PlayerController {

	PlayerAPI PlayerApi { get; }

	bool InputEnabled { get; }

	void StarNewGame();

	void EnableInput();

	void DisableInput();
}