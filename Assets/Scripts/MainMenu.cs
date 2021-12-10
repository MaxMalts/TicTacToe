using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using Network;



public class MainMenu : Unique<MainMenu> {

	const string connectingPopupMessage = "Connecting to other player...";

	[SerializeField] GameObject canvas;

	[SerializeField] [Tooltip("Main Canvas Group of main menu.")]
	CanvasGroup uiCanvasGroup;

	const string myCellSignQuery = "my-cell-sign";
	const string crossSignValue = "cross";
	const string noughtSignValue = "nought";

	PeerToPeerClient ptpClient;
	Task connectingTask;
	bool connected;
	bool needToDoConnecting = false;
	CellSign localCellSign;


	public void ConnectAndStartGame(CellSign sign) {
		if (connectingTask?.Status == TaskStatus.Running) {
			return;
		}

		localCellSign = sign;

		GameObject ptpObject =
			new GameObject("PeerToPeerClient", typeof(PeerToPeerClient));
		ptpClient = ptpObject.GetComponent<PeerToPeerClient>();
		DontDestroyOnLoad(ptpClient);

		needToDoConnecting = true;
	}

	void Awake() {
		Assert.IsNotNull(canvas, "Canvas was not assigned in inspector.");
		Assert.IsNotNull(uiCanvasGroup, "UI Canvas Group was not assigned in inspector.");
	}

	void Update() {
		if (needToDoConnecting) {
			needToDoConnecting = false;
			DoConnecting();
		}
		if (connected) {
			SceneArgsManager.NextSceneArgs.Add("cell-sign", localCellSign);
			SceneArgsManager.NextSceneArgs.Add("ptp-client", ptpClient);
			SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
		}
	}

	async void ShowNoWifiPopup() {
		ButtonPopupController popup = PopupsManager.ShowConfirmPopup("Check your WiFi connection.");
		await popup.WaitForClickOrCloseAsync();
		popup.Close();
	}

	void DoConnecting() {
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
			PopupsManager.ShowLoadingCancelPopup(connectingPopupMessage);

			uiCanvasGroup.interactable = false;

			connectingTask.ContinueWith((Task) => {
				if (Task.Status != TaskStatus.RanToCompletion) {
					Debug.LogError("connectingTask not completed successfully. Its status: " +
						connectingTask.Status + '.');
					return;
				}

				string signValue =
					localCellSign == CellSign.Cross ? crossSignValue : noughtSignValue;

				try {
					ptpClient.Send(Encoding.UTF8.GetBytes(myCellSignQuery + ':' + signValue));
				} catch (NotConnectedException) {
					needToDoConnecting = true;
				}

				ptpClient.PackageReceived.AddListener(OnPackageReceived);
				ptpClient.StartReceiving();
			});

		} else {
			ShowNoWifiPopup();
		}
	}

	void OnPackageReceived(byte[] data) {
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
			} else {
				Debug.Log("Other player had same sign. Reconnecting.");
				ptpClient.Disconnect();
				needToDoConnecting = true;
			}

		} else {
			Debug.LogError("Bad query received: \"" + queryStr + "\".");
		}
	}
}
