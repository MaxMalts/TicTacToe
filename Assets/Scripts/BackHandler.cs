using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;



public class BackHandler : Singleton<BackHandler> {

	public UnityEvent OnBack { get; } = new UnityEvent();


	public void Update() {
		// Input actions escape key does not work >:(
		if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasReleasedThisFrame) {
			OnBack.Invoke();
		}
	}
}