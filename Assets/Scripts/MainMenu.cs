﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using Network;



public class MainMenu : MonoBehaviour {

	static MainMenu instance;
	public static MainMenu Instance {
		get {
			Assert.IsNotNull<MainMenu>(instance, "No instance of MainMenu.");
			return instance;
		}

		private set {
			instance = value;
		}
	}

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

		connectingTask = ptpClient.ConnectToOtherClient();
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
	}

	void Update() {
		if (connected) {
			SceneArgsManager.NextSceneArgs.Add("cell-sign", localCellSign);
			SceneArgsManager.NextSceneArgs.Add("ptp-client", ptpClient);
			SceneManager.LoadScene((int)SceneIndeces.TicTacToe);
		}
	}

	void OnEnable() {
		Assert.IsNull<MainMenu>(instance, "You've enabled multiple MainMenyAPI's.");
		Instance = this;
	}

	void OnDisable() {
		Instance = null;
	}
}
