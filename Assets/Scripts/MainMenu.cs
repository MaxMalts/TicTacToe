﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using Network;



/// <summary>
/// Compared to MainMenuAPI, does not know anything about UI and is used as a
/// lower level abstraction. All main menu features logic is here.
/// </summary>

public class MainMenu : Unique<MainMenu> {

	volatile bool connecting = false;
	public bool Connecting {
		get {
			return connecting;
		}
	}

	const string connectingPopupMessage = "Connecting to other player...";
	const string noWifiMessage = "Check your WiFi connection.";

	const string myCellSignQuery = "my-cell-sign";
	const string crossSignValue = "cross";
	const string noughtSignValue = "nought";

	[SerializeField] GameObject canvas;

	PeerToPeerClient ptpClient;
	Task connectingTask;
	bool connected;
	volatile bool needToSendSign = false;
	CellSign localCellSign;


	public void StartGame(GameMode gameMode, GameDifficulty? difficulty = null, CellSign? cellSign = null) {
		switch (gameMode) {
			case GameMode.Singleplayer:
				Assert.IsTrue(difficulty != null, "gameMode singleplayer but difficulty is null.");
				StartSingleplayer(difficulty.Value);
				break;

			case GameMode.Multiplayer:
				StartMultiplayer();
				break;

			case GameMode.WifiMultiplayer:
				Assert.IsTrue(cellSign != null, "gameMode multiplayer but cellSign is null.");
				StartWifiMultiplayer(cellSign.Value);
				break;

			default:
				Assert.IsTrue(false, "Bad gameMode value.");
				break;
		}
	}

	void StartSingleplayer(GameDifficulty difficulty) {
		SceneArgsManager.NextSceneArgs.Add("game-mode", GameMode.Singleplayer);
		SceneArgsManager.NextSceneArgs.Add("game-difficulty", difficulty);
		SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
	}

	void StartMultiplayer() {
		SceneArgsManager.NextSceneArgs.Add("game-mode", GameMode.Multiplayer);
		SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
	}

	void StartWifiMultiplayer(CellSign sign) {
		ShowConnectingPopup();

		localCellSign = sign;

		if (!connecting) {
			GameObject ptpObject =
				new GameObject("PeerToPeerClient", typeof(PeerToPeerClient));
			ptpClient = ptpObject.GetComponent<PeerToPeerClient>();
			DontDestroyOnLoad(ptpClient);

			ptpClient.PackageReceived.AddListener(OnPackageReceived);
			ptpClient.Disconnected.AddListener(OnDisconnected);
			DoConnecting();
		}
	}

	public void Quit() {
		if (connecting) {
			CancelConnecting();
		}

		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}

	void Awake() {
		Assert.IsNotNull(canvas, "Canvas was not assigned in inspector.");
	}

	void Update() {
		if (needToSendSign) {
			needToSendSign = false;
			string signValue =
				localCellSign == CellSign.Cross ? crossSignValue : noughtSignValue;
			try {
				ptpClient.Send(Encoding.UTF8.GetBytes(myCellSignQuery + ':' + signValue));
			} catch (NotConnectedException) {
				DoConnecting();
				return;
			}

			ptpClient.StartReceiving();
		}

		if (connected) {
			SceneArgsManager.NextSceneArgs.Add("game-mode", GameMode.WifiMultiplayer);
			SceneArgsManager.NextSceneArgs.Add("local-cell-sign", localCellSign);
			SceneArgsManager.NextSceneArgs.Add("ptp-client", ptpClient);
			SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
		}
	}

	void CancelConnecting() {
		Assert.IsNotNull(ptpClient);
		Destroy(ptpClient.gameObject);
		ptpClient = null;

		needToSendSign = false;
		connected = false;
		connecting = false;
		localCellSign = CellSign.Empty;
	}

	void DoConnecting() {
		connecting = true;
		bool networkAvailable = true;
		if (PeerToPeerClient.NetworkAvailable) {
			try {
				connectingTask = ptpClient.ConnectToOtherClient();
			} catch (NoNetworkException) {
				networkAvailable = false;
			}

		} else {
			networkAvailable = false;
		}

		if (networkAvailable) {
			connectingTask.ContinueWith((task) => {
				if (task.Status != TaskStatus.RanToCompletion &&
					task.Status != TaskStatus.Canceled &&
					task.Status != TaskStatus.Faulted) {

					Debug.LogError("connectingTask has bad status: " + connectingTask.Status + '.');
					return;
				}
				if (task.Status == TaskStatus.Canceled) {
					return;
				}
				if (task.Status == TaskStatus.Faulted &&
					(task.Exception.InnerExceptions.Count != 1 ||
					!(task.Exception.InnerExceptions[0] is ObjectDisposedException))) {

					Debug.LogException(task.Exception, this);
					return;
				}

				if (task.Status == TaskStatus.RanToCompletion) {
					Debug.Log("Connected");
					needToSendSign = true;
				}
			});

		} else {
			ShowNoWifiPopup();
		}
	}

	async void ShowNoWifiPopup() {
		ButtonPopupController popup = PopupsManager.ShowConfirmPopup(noWifiMessage);
		await popup.WaitForClickOrCloseAsync();
		popup.Close();
	}

	async void ShowConnectingPopup() {
		ButtonPopupController popup = PopupsManager.ShowLoadingCancelPopup(connectingPopupMessage);
		bool canceled = await popup.WaitForClickOrCloseAsync();
		if (canceled) {
			CancelConnecting();
		}
		popup.Close();
	}

	void OnPackageReceived(byte[] data) {
		Debug.Log("There");
		ptpClient.StopReceiving();

		string queryStr = Encoding.UTF8.GetString(data);

		if (queryStr.StartsWith(myCellSignQuery) &&
			queryStr.Length > myCellSignQuery.Length + 1) {

			Assert.IsTrue(localCellSign == CellSign.Cross ||
				localCellSign == CellSign.Nought);

			string expectedSignValue =
				localCellSign == CellSign.Cross ? noughtSignValue : crossSignValue;

			if (expectedSignValue == queryStr.Substring(myCellSignQuery.Length + 1)) {
				connected = true;
				ptpClient.PackageReceived.RemoveListener(OnPackageReceived);

			} else {
				Debug.Log("Other player had same sign. Reconnecting.");
				DoConnecting();
			}

		} else {
			Debug.LogError("Bad query received: \"" + queryStr + "\".");
		}
	}

	void OnDisconnected() {
		needToSendSign = false;
		connected = false;
		DoConnecting();
	}
}
