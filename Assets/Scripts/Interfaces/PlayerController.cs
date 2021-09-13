
public interface PlayerController {

	PlayerAPI PlayerApi { get; }

	void StartGame(CellSign sign);

	void EnableInput();

	void DisableInput();
}