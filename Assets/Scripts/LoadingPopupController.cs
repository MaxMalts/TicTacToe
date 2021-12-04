using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;



/// <summary>
/// Controller for popup with loading icon.
/// </summary>
public class LoadingPopupController : Singleton<LoadingPopupController> {

	[SerializeField] GameObject loadingPopupPrefab;

	GameObject popupParent;
	GameObject popup;
	TextMeshProUGUI messageTextMesh;


	public static void DisplayMessage(string message) {
		if (Instance.popup != null) {
			Assert.IsNotNull(Instance.messageTextMesh,
				"Popup null but message text mesh null.");

			Instance.messageTextMesh.text = message;
			return;
		}

		if (Instance.popupParent == null) {
			Canvas canvas = FindOverlayCanvas();
			if (canvas != null) {
				Instance.popupParent = canvas.gameObject;
			}
		}

		if (Instance.popupParent == null) {
			Debug.LogWarning("There is no UI GameObject to which popup can be " +
				"attached (no UI object is provided and no overlay canvas was detected.");
			return;
		}

		Instance.popup =
			Instantiate(Instance.loadingPopupPrefab, Instance.popupParent.transform);
		Instance.messageTextMesh =
			Instance.popup.GetComponent<LoadingPopupTextVar>().MessageTextMesh;
		Instance.messageTextMesh.text = message;
	}

	public static void RemovePopup() {
		if (Instance.popup != null) {
			Destroy(Instance.popup);
			Instance.popup = null;
			Instance.messageTextMesh = null;
		}
	}

	public static void SetParent(GameObject parent) {
		if (parent.GetComponent<RectTransform>() == null) {
			Debug.LogError("Parent of loading popup must be a UI object.", Instance);
			return;
		}

		parent = Instance.popupParent;
	}

	protected override void Awake() {
		base.Awake();

		Assert.IsNotNull(loadingPopupPrefab,
			"Loading Popup prefab was not assigned in inspector.");
	}

	static Canvas FindOverlayCanvas() {
		Canvas[] canvases = FindObjectsOfType<Canvas>();

		foreach (Canvas canvas in canvases) {
			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
				return canvas;
			}
		}

		return null;
	}
}
