using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Network;



[RequireComponent(typeof(PlayerAPI))]
public class RemotePlayerController : MonoBehaviour, PlayerController {

	public PlayerAPI PlayerApi { get; private set; }

	const string startGameQuery = "start-game";

	const string remoteCellSignQuery = "cell-sign";
	const string crossSignValue = "cross";
	const string noughtSignValue = "nought";

	const string placeCellQuery = "place-cell";

	PeerToPeerClient ptpClient;
	bool inputEnabled = true;
	bool localStartPending = false;
	bool remoteStartPending = false;
	bool inited = false;

	GameManager gameManager;


	public void EnableInput() {
		inputEnabled = true;
	}

	public void DisableInput() {
		inputEnabled = false;
	}

	public void StartGame(CellSign sign) {
		Assert.IsTrue(sign == CellSign.Cross || sign == CellSign.Nought);
		Assert.IsNotNull(ptpClient);

		if (!inited) {
			Init();
			inited = true;
		}

		PlayerApi.Sign = sign;
		inputEnabled = false;

		Assert.IsNotNull(gameManager);

		PlayerAPI localPlayerApi =
			gameManager.LocalPlayer.GetComponent<PlayerAPI>();
		Assert.IsNotNull(localPlayerApi,
			"No PlayerAPI component on local player GameObject.");

		if (remoteStartPending) {
			Assert.IsFalse(localStartPending, "Already pending local game start.");

			remoteStartPending = false;
			SendSign();

		} else {
			localStartPending = true;
		}
	}

	void Awake() {
		PlayerApi = GetComponent<PlayerAPI>();
		Assert.IsNotNull(PlayerApi, "No PlayerAPI script on PlayerApi.");
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

		PlayerAPI localPlayerApi =
			gameManager.LocalPlayer.GetComponent<PlayerAPI>();
		Assert.IsNotNull(localPlayerApi,
			"No PlayerAPI component on local player GameObject.");

		localPlayerApi.CellPlaced.AddListener(OnLocalCellPlaced);

		ptpClient.PackageReceived.AddListener(OnPackageReceived);
		ptpClient.StartReceiving();
	}

	void OnLocalCellPlaced(PlayerAPI.PlaceContext placeContext) {
		Assert.IsTrue(placeContext.PlayerType == PlayerAPI.PlayerType.Local);
		Assert.IsTrue(placeContext.Sign != PlayerApi.Sign);

		string queryStr =
			placeCellQuery + ':' + placeContext.FieldPos.x.ToString() +
			',' + placeContext.FieldPos.y.ToString();

		ptpClient.Send(Encoding.UTF8.GetBytes(queryStr));
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

	void HandleStartGameQuery() {
		if (localStartPending) {
			Assert.IsFalse(remoteStartPending, "Already pending local game start.");

			localStartPending = false;
			SendSign();

		} else {
			localStartPending = true;
		}
	}

	void HandleCellSignQuery(string value) {
		Assert.IsTrue(PlayerApi.Sign == CellSign.Cross ||
			PlayerApi.Sign == CellSign.Nought);

		string localSignValue =
			PlayerApi.Sign == CellSign.Cross ? noughtSignValue : crossSignValue;
		if (localSignValue != value) {
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

		Assert.IsTrue(inputEnabled, "Remote player placed cell when input was disabled.");
		PlayerApi.Place(new Vector2Int(column, row));
	}

	void SendSign() {
		string signValue =
				PlayerApi.Sign == CellSign.Cross ? crossSignValue : noughtSignValue;
		ptpClient.Send(Encoding.UTF8.GetBytes(remoteCellSignQuery + ':' + signValue));
	}
}
