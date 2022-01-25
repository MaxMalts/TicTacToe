using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.InputSystem;
using UnityEngine.Events;



public class BackHandler : Singleton<BackHandler> {

	public UnityEvent OnBack { get; } = new UnityEvent();

	//class BackPressCallback : AndroidJavaProxy {

	//	UnityEvent onBack;

	//	public BackPressCallback(UnityEvent onBack) : base("androidx.activity.OnBackPressedCallback") {
	//		this.onBack = onBack;
	//	}

	//	public void handleOnBackPressed() {
	//		Debug.Log("Back pressed.");
	//		onBack.Invoke();
	//	}
	//}

	//void Start() {
	//	InputSystem.onActionChange += (object arg1, InputActionChange arg2) => Debug.Log("Action change: arg1: " + arg1.ToString() + " arg2: " + arg2.ToString());
	//	backAction.Enable();
	//	backAction.performed += (InputAction.CallbackContext context) => Debug.Log("Back performed");
	//	backAction.started += (InputAction.CallbackContext context) => Debug.Log("Back started");
	//	backAction.canceled += (InputAction.CallbackContext context) => Debug.Log("Back canceled");
	//}

	public void Update() {
		// New input system doesn't work with android back button correctly >:(
		// Uncommend the code in this file to see what I tried.
		if (Input.GetKeyDown(KeyCode.Escape)) {
			OnBack.Invoke();
		}

		//var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		//var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
		//var dispatcher = activity.Call<AndroidJavaObject>("getOnBackPressedDispatcher");
		//dispatcher.Call<BackPressCallback>("addCallback", new BackPressCallback(OnBack));

		//if (Keyboard.current.escapeKey.wasReleasedThisFrame) {
		//	OnBack.Invoke();
		//}
	}
}