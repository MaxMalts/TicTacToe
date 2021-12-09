using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;



public class PopupsManager : Singleton<PopupsManager> {

	public PopupController ActivePopup { get; private set; }

	[SerializeField] GameObject simplePopupPrefab;
	[SerializeField] GameObject confirmPopupPrefab;
	[SerializeField] GameObject loadingPopupPrefab;


	public static void ShowSimplePopup(string message, GameObject parent = null) {
		ShowPopup(Instance.simplePopupPrefab, message, parent);
	}

	public static void ShowConfirmPopup(string message, string buttonLabel = "Ok", GameObject parent = null) {
		ShowPopup(Instance.simplePopupPrefab, message, parent);
	}

	public static void ShowLoadingPopup(string message, GameObject parent = null) {
		ShowPopup(Instance.loadingPopupPrefab, message, parent);
	}

	public static void CloseActivePopup() {
		if (Instance.ActivePopup != null) {
			Instance.ActivePopup.Close();
		}
	}

	protected override void Awake() {
		base.Awake();

		Assert.IsNotNull(simplePopupPrefab, "Popup prefab was not assigned in inspector.");
		Assert.IsNotNull(confirmPopupPrefab, "Popup prefab was not assigned in inspector.");
		Assert.IsNotNull(loadingPopupPrefab, "Loading Popup prefab was not assigned in inspector.");
	}

	static void ShowPopup(GameObject prefab, string message, GameObject parent) {
		Assert.IsNotNull(prefab);

		Instance.ActivePopup.Close();
		Assert.IsNull(Instance.ActivePopup, "Closed popup but ActivePopup was not set to null.");

		if (parent != null && parent.GetComponent<RectTransform>() == null) {
			Debug.LogWarning("Parent of popup must be a UI object. " +
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

		GameObject curPopupObj = Instantiate(prefab, parent.transform);
		Instance.ActivePopup = curPopupObj.GetComponent<PopupController>();
		Assert.IsNotNull(Instance.ActivePopup, "No PopupController on popup instance.");

		Instance.ActivePopup.MessageTextMesh.text = message;
		Instance.ActivePopup.PopupClosing.AddListener(Instance.OnPopupClosing);
	}

	void OnPopupClosing() {
		Assert.IsNotNull(Instance.ActivePopup, "Popup closed but ActivePopup was null in PopupsManager.");
		Instance.ActivePopup = null;
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
