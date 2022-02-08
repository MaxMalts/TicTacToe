using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class WifiMultiplayerHelpButtonController : MonoBehaviour {

	public async void OnClick() {
		ButtonPopupController popup = PopupsManager.ShowConfirmPopup(
			"To play with your friend over the network, connect with your friend to " +
			"the same WiFi or hotspot and click on different signs.");

		await popup.WaitForClickOrCloseAsync();
		popup.Close();
	}
}
