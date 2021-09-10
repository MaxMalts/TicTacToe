using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using Network;



public class MainMenu : Unique<MainMenu> {

	[SerializeField] [Tooltip("Main Canvas Group of main menu.")]
	CanvasGroup uiCanvasGroup;

	[SerializeField] GameObject connectingPopup;

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
			connectingPopup.SetActive(true);
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

	void Update() {
		if (connected) {
			SceneArgsManager.NextSceneArgs.Add("cell-sign", localCellSign);
			SceneArgsManager.NextSceneArgs.Add("ptp-client", ptpClient);
			SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
		}
	}
}
