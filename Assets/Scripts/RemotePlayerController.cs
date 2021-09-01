﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Network;



public class RemotePlayerController : MonoBehaviour, PlayerController {

	public PlayerAPI PlayerApi { get; private set; }

	PeerToPeerClient ptpClient;
	bool inputEnabled = true;

	string remoteCellSignQuery = "cell-sign";
	string crossSignValue = "cross";
	string noughtSignValue = "nought";

	string placeCellQuery = "place-cell";


	public void EnableInput() {
		inputEnabled = true;
	}

	public void DisableInput() {
		inputEnabled = false;
	}

	void Awake() {
		object ptpClientObj;
		if (!SceneArgsManager.CurSceneArgs.TryGetValue("ptp-client", out ptpClientObj)) {
			Debug.LogError("No ptp-client passed to current scene.");
			Assert.IsNotNull(ptpClientObj);
		}
		try {
			ptpClient = (PeerToPeerClient)ptpClientObj;
		} catch (InvalidCastException innerException) {
			throw new ArgumentException("Bad value passed to scene.",
				"ptp-client", innerException);
		}
	}

	void Start() {
		Assert.IsTrue(PlayerApi.Sign == CellSign.Cross ||
			PlayerApi.Sign == CellSign.Nought);

		ptpClient.packageReceived.AddListener(OnPackageReceived);
		ptpClient.StartReceiving();

		string signValue =
			PlayerApi.Sign == CellSign.Cross ? crossSignValue : noughtSignValue;
		ptpClient?.Send(Encoding.UTF8.GetBytes(remoteCellSignQuery + ':' + signValue));
	}

	void OnPackageReceived(byte[] data) {


		string queryStr = Encoding.UTF8.GetString(data);
		if (queryStr.StartsWith(remoteCellSignQuery) &&
			queryStr.Length > remoteCellSignQuery.Length + 1) {

			HandleCellSignQuery(queryStr.Substring(remoteCellSignQuery.Length + 1));

		} else if (queryStr.StartsWith(placeCellQuery) &&
			queryStr.Length > placeCellQuery.Length + 1) {

			HandlePlaceCellQuery(queryStr.Substring(placeCellQuery.Length + 1));

		} else {
			Debug.LogError("Bad query received: \"" + queryStr + "\".");
		}

	}

	void HandleCellSignQuery(string value) {
		Assert.IsTrue(PlayerApi.Sign == CellSign.Cross ||
			PlayerApi.Sign == CellSign.Nought);

		string targetSignValue =
			PlayerApi.Sign == CellSign.Cross ? crossSignValue : noughtSignValue;
		if (targetSignValue == value) {
			return;
		}

		Debug.LogError("Remote player sign mismatch. value received: \"" +
			value + "\".");
	}

	void HandlePlaceCellQuery(string value) {
		bool badValue = false;
		int column;
		if (!int.TryParse(value.Substring(0, value.IndexOf(',') + 1), out column)) {
			badValue = true;
		}
		int row;
		if (!int.TryParse(value.Substring(0, value.IndexOf(',') + 1), out row)) {
			badValue = true;
		}
		if (badValue) {
			Debug.LogError("Bad cell position received: \"" + value + "\"");
			return;
		}

		PlayerApi.Place(new Vector2Int(column, row));
	}
}
