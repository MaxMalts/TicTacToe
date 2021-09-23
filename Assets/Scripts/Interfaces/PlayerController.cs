
public interface PlayerController {

	PlayerAPI PlayerApi { get; }

	bool InputEnabled { get; }

	void StartGame(CellSign sign);

	void EnableInput();

	void DisableInput();
}