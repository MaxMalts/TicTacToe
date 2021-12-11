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
	const string noWifiMessage = "Check your WiFi connection.";

	[SerializeField] GameObject canvas;

	const string myCellSignQuery = "my-cell-sign";
	const string crossSignValue = "cross";
	const string noughtSignValue = "nought";

	PeerToPeerClient ptpClient;
	Task<bool> connectingTask;
	bool connected;
	volatile bool needToSendSign = false;
	volatile bool needToCancelConnecting = false;
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

		ptpClient.PackageReceived.AddListener(OnPackageReceived);
		ptpClient.Disconnected.AddListener(OnDisconnected);
		DoConnecting();
	}

	public void CancelConnecting() {
		needToSendSign = false;
		connected = false;
		localCellSign = CellSign.Empty;
		Destroy(ptpClient.gameObject);
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

		if (needToCancelConnecting) {
			needToCancelConnecting = false;
			CancelConnecting();
		}

		if (connected) {
			SceneArgsManager.NextSceneArgs.Add("cell-sign", localCellSign);
			SceneArgsManager.NextSceneArgs.Add("ptp-client", ptpClient);
			SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
		}
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
			ShowConnectingPopup();

			connectingTask.ContinueWith((task) => {
				if (task.Status != TaskStatus.RanToCompletion) {
					Debug.LogError("connectingTask not completed successfully. Its status: " +
						connectingTask.Status + '.');
					return;
				}

				if (task.Result) {
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
			needToCancelConnecting = true;
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
