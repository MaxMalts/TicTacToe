using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class BackButtonHandler : MonoBehaviour {

	public void OnBack() {
		MainMenuAPI.Instance.Back();
	}

	void Update() {
		// Input actions escape key does not work >:(
		if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasReleasedThisFrame) {
			OnBack();
		}
	}
}