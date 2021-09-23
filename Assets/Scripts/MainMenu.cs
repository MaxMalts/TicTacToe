using System;
using System.Collections;
using System.Collections.Generic;
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

	[SerializeField] GameObject loadingPopupPrefab;
	LoadingPopupAPI loadingPopupApi;

	PeerToPeerClient ptpClient;
	Task connectingTask;
	bool connected;
	CellSign localCellSign;


	public void ConnectAndStartGame(CellSign sign) {
		if (connectingTask?.Status == TaskStatus.Running) {
			return;
		}

		GameObject ptpObject =
			new GameObject("PeerToPeerClient", typeof(PeerToPeerClient));
		ptpClient = ptpObject.GetComponent<PeerToPeerClient>();
		DontDestroyOnLoad(ptpClient);

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
			GameObject loadingPopupObject = Instantiate(loadingPopupPrefab, canvas.transform);
			loadingPopupApi = loadingPopupObject.GetComponent<LoadingPopupAPI>();
			Assert.IsNotNull(loadingPopupApi,
				"No LoadingPopupAPI script on Loading Popup GameObject.");
			loadingPopupApi.Message = connectingPopupMessage;

			uiCanvasGroup.interactable = false;

			connectingTask.ContinueWith((Task) => {
				if (Task.Status != TaskStatus.RanToCompletion) {
					Debug.LogError("connectingTask not completed successfully. Its status: " +
						connectingTask.Status + '.');
					return;
				}

				localCellSign = sign;
				connected = true;
			});

		} else {
			Debug.Log("No wifi.");
		}
	}

	void Awake() {
		Assert.IsNotNull(canvas, "Canvas was not assigned in inspector.");
		Assert.IsNotNull(uiCanvasGroup, "UI Canvas Group was not assigned in inspector.");
		Assert.IsNotNull(loadingPopupPrefab,
			"Loading Popup prefab was not assigned in inspector.");
	}

	void Update() {
		if (connected) {
			SceneArgsManager.NextSceneArgs.Add("cell-sign", localCellSign);
			SceneArgsManager.NextSceneArgs.Add("ptp-client", ptpClient);
			SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
		}
	}
}
