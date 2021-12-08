using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;



public class PopupsManager : Singleton<PopupsManager> {

	[SerializeField] GameObject simplePopupPrefab;
	[SerializeField] GameObject loadingPopupPrefab;

	GameObject curPopup;
	TextMeshProUGUI curPopupTextMesh;


	public static void ShowSimplePopup(string message, GameObject parent = null) {
		ShowPopup(Instance.simplePopupPrefab, message, parent);
	}

	public static void ShowLoadingPopup(string message, GameObject parent = null) {
		ShowPopup(Instance.loadingPopupPrefab, message, parent);
	}

	public static void RemovePopup() {
		if (Instance.curPopup != null) {
			Destroy(Instance.curPopup);
			Instance.curPopup = null;
			Instance.curPopupTextMesh = null;
		}
	}

	protected override void Awake() {
		base.Awake();

		Assert.IsNotNull(simplePopupPrefab, "Popup prefab was not assigned in inspector.");
		Assert.IsNotNull(loadingPopupPrefab, "Loading Popup prefab was not assigned in inspector.");
	}

	static void ShowPopup(GameObject prefab, string message, GameObject parent) {
		Assert.IsNotNull(prefab);

		RemovePopup();

		if (parent != null && parent.GetComponent<RectTransform>() == null) {
			Debug.LogWarning("Parent of loading popup must be a UI object. " +
				"Overlay canvas will be searched for.");

			parent = null;
		}

		if (parent == null) {
			Canvas canvas = FindOverlayCanvas();
			if (canvas != null) {
				parent = canvas.gameObject;
			}
		}

		if (parent == null) {
			Debug.LogWarning("There is no UI GameObject to which popup can be " +
				"attached (no UI parent is provided and no overlay canvas was detected. " +
				"Popup will not be shown");

			return;
		}

		Instance.curPopup =
			Instantiate(prefab, parent.transform);
		Instance.curPopupTextMesh =
			Instance.curPopup.GetComponent<PopupTextVar>()?.MessageTextMesh;
		Assert.IsNotNull(Instance.curPopupTextMesh, "No text mesh on popup instance.");

		Instance.curPopupTextMesh.text = message;
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
