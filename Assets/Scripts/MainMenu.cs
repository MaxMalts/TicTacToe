using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class MainMenu : MonoBehaviour {

	CanvasGroup uiGroup;
	GameObject connectingPopup;


	void Awake() {
		CanvasGroup[] canvasGroups = FindObjectsOfType<CanvasGroup>();
		Assert.IsTrue(canvasGroups.Length == 1, "Not single CanvasGroup object");
		uiGroup = canvasGroups[0];

		connectingPopup = 
	}
}
