using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Network;



[RequireComponent(typeof(PlayerAPI))]
public class RemotePlayerController : MonoBehaviour, PlayerController {

	public PlayerAPI PlayerApi { get; private set; }

	public bool InputEnabled { get; private set; } = false;

	const string waitingForPlayerMessage = "Waiting for other player...";

	const string startGameQuery = "start-game";

	const string remoteCellSignQuery = "cell-sign";
	const string crossSignValue = "cross";
	const string noughtSignValue = "nought";

	const string placeCellQuery = "place-cell";

	PeerToPeerClient ptpClient;
	bool localStartHappened = false;
	bool remoteStartHappened = false;
	bool inited = false;

	GameManager gameManager;


	public void EnableInput() {
		InputEnabled = true;
	}

	public void DisableInput() {
		InputEnabled = false;
	}

	public void StarNewGame() {
		Assert.IsTrue(PlayerApi.Sign == CellSign.Cross || PlayerApi.Sign == CellSign.Nought);
		Assert.IsNotNull(ptpClient);

		if (!inited) {
			Init();
			inited = true;
		}

		Assert.IsNotNull(gameManager);

		Assert.IsTrue(ReferenceEquals(this, gameManager.Player1) || ReferenceEquals(this, gameManager.Player2));
		PlayerController otherPlayer =
			ReferenceEquals(this, gameManager.Player1) ? gameManager.Player2 : gameManager.Player1;
		Assert.IsNotNull(otherPlayer);

		PlayerAPI otherPlayerApi = otherPlayer.PlayerApi;
		Assert.IsNotNull(otherPlayerApi);

		otherPlayerApi.CellPlaced.RemoveListener(OnLocalCellPlaced);
		otherPlayerApi.CellPlaced.AddListener(OnLocalCellPlaced);

		DisableInput();

		try {
			ptpClient.StartReceiving();
			Debug.Log("Started");
			ptpClient.Send(Encoding.UTF8.GetBytes(startGameQuery));
		} catch (NotConnectedException) {
			OnDisconnected();
		}

		if (remoteStartHappened) {
			Assert.IsFalse(localStartHappened, "Local game start already happened.");

			remoteStartHappened = false;
			SendSign();

		} else {
			localStartHappened = true;
			gameManager.SuspendGame();
			PopupsManager.ShowLoadingPopup(waitingForPlayerMessage);
		}
	}

	void Awake() {
		PlayerApi = GetComponent<PlayerAPI>();
		Assert.IsNotNull(PlayerApi, "No PlayerAPI script on RemotePlayerController.");
		PlayerApi.Type = PlayerAPI.PlayerType.Remote;

		object ptpClientObj;
		if (!SceneArgsManager.CurSceneArgs.TryGetValue("ptp-client", out ptpClientObj)) {
			Debug.LogError("No ptp-client passed to current scene.");
		}
		try {
			ptpClient = (PeerToPeerClient)ptpClientObj;
		} catch (InvalidCastException innerException) {
			throw new ArgumentException("Bad value passed to scene.",
				"ptp-client", innerException);
		}

		GameManager.Instance.ReturningToMainMenu.AddListener(OnReturningToMainMenu);
	}

	void Start() {
		if (!inited) {
			Init();
			inited = true;
		}
	}

	void Init() {
		gameManager = GameManager.Instance;
		Assert.IsNotNull(gameManager);

		ptpClient.PackageReceived.AddListener(OnPackageReceived);
		ptpClient.Disconnected.AddListener(OnDisconnected);
	}

	void OnReturningToMainMenu() {
		Destroy(ptpClient.gameObject);
	}

	void OnLocalCellPlaced(PlayerAPI.PlaceContext placeContext) {
		Assert.IsTrue(placeContext.PlayerType == PlayerAPI.PlayerType.User);
		Assert.IsTrue(placeContext.Sign != PlayerApi.Sign);

		string queryStr =
			placeCellQuery + ':' + placeContext.FieldPos.x.ToString() +
			',' + placeContext.FieldPos.y.ToString();

		try {
			ptpClient.Send(Encoding.UTF8.GetBytes(queryStr));
		} catch (NotConnectedException) {
			return;
		}
	}

	void OnPackageReceived(byte[] data) {
		string queryStr = Encoding.UTF8.GetString(data);

		if (queryStr.StartsWith(startGameQuery) &&
			queryStr.Length == startGameQuery.Length) {

			HandleStartGameQuery();

		} else if (queryStr.StartsWith(remoteCellSignQuery) &&
			queryStr.Length > remoteCellSignQuery.Length + 1) {

			HandleCellSignQuery(queryStr.Substring(remoteCellSignQuery.Length + 1));

		} else if (queryStr.StartsWith(placeCellQuery) &&
			queryStr.Length > placeCellQuery.Length + 1) {

			HandlePlaceCellQuery(queryStr.Substring(placeCellQuery.Length + 1));

		} else {
			Debug.LogError("Bad query received: \"" + queryStr + "\".");
		}
	}

	async void OnDisconnected() {
		Debug.Log("Player disconnnected.");

		GameManager.Instance.SuspendGame();

		ButtonPopupController confirmPopup = PopupsManager.ShowConfirmPopup("Player disconnected");
		await confirmPopup.WaitForClickOrCloseAsync();
		GameManager.Instance.ReturnToMainMenu();
	}

	void HandleStartGameQuery() {
		if (localStartHappened) {
			Assert.IsFalse(remoteStartHappened, "Remote game start already happened.");

			localStartHappened = false;
			SendSign();
			PopupsManager.CloseActivePopup();
			gameManager.UnsuspendGame();

		} else {
			remoteStartHappened = true;
		}
	}

	void HandleCellSignQuery(string value) {
		Assert.IsTrue(PlayerApi.Sign == CellSign.Cross || PlayerApi.Sign == CellSign.Nought);

		string otherPlayerSignValue =
			PlayerApi.Sign == CellSign.Cross ? noughtSignValue : crossSignValue;
		if (otherPlayerSignValue != value) {
			Debug.LogError("Remote player sign mismatch. value received: \"" +
				value + "\".");
		}
	}

	void HandlePlaceCellQuery(string value) {
		bool badValue = false;
		int column;
		if (!int.TryParse(value.Substring(0, value.IndexOf(',')), out column)) {
			badValue = true;
		}
		int row;
		if (!int.TryParse(value.Substring(value.IndexOf(',') + 1), out row)) {
			badValue = true;
		}
		if (badValue) {
			Debug.LogError("Bad cell position received: \"" + value + "\"");
			return;
		}

		Assert.IsTrue(InputEnabled, "Remote player placed cell when input was disabled.");
		PlayerApi.Place(new Vector2Int(column, row));
	}

	void SendSign() {
		string signValue =
			PlayerApi.Sign == CellSign.Cross ? crossSignValue : noughtSignValue;

		try {
			ptpClient.Send(Encoding.UTF8.GetBytes(remoteCellSignQuery + ':' + signValue));
		} catch (NotConnectedException) {
			return;
		}
	}
}
