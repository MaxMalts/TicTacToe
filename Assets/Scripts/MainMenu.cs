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
	Task connectingTask;
	bool connected;
	volatile bool needToSendSign = false;
	volatile bool needToCancelConnecting = false;
	volatile bool connectionCanceled = true;
	CellSign localCellSign;


	public void ConnectAndStartGame(CellSign sign) {
		ShowConnectingPopup();

		localCellSign = sign;

		if (connectionCanceled) {
			GameObject ptpObject =
				new GameObject("PeerToPeerClient", typeof(PeerToPeerClient));
			ptpClient = ptpObject.GetComponent<PeerToPeerClient>();
			DontDestroyOnLoad(ptpClient);

			ptpClient.PackageReceived.AddListener(OnPackageReceived);
			ptpClient.Disconnected.AddListener(OnDisconnected);
			DoConnecting();
		}
	}

	void Awake() {
		Assert.IsNotNull(canvas, "Canvas was not assigned in inspector.");
	}

	void Update() {
		if (needToCancelConnecting) {
			needToCancelConnecting = false;
			CancelConnecting();
		}

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
			SceneArgsManager.NextSceneArgs.Add("cell-sign", localCellSign);
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
		connectionCanceled = true;
		localCellSign = CellSign.Empty;
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
