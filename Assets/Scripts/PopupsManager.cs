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


	/// <summary>
	/// Shows popup with just a text in it.
	/// </summary>
	/// <param name="message">Message to be displayed</param>
	/// <param name="parent">
	/// Ui obeject to whick popup will be attached.
	/// If null, then overlay canvas will be automatically searched for
	/// </param>
	/// <returns>Created popup</returns>
	public static PopupController ShowSimplePopup(string message, GameObject parent = null) {
		return ShowPopup(Instance.simplePopupPrefab, message, parent);
	}

	/// <summary>
	/// Shows popup with a text and confirmation button in it.
	/// </summary>
	/// <param name="message">Message to be displayed</param>
	/// <param name="buttonLabel">Text inside the confirmation button</param>
	/// <param name="parent">
	/// Ui obeject to whick popup will be attached.
	/// If null, then overlay canvas will be automatically searched for
	/// </param>
	/// <returns>Created popup</returns>
	public static ConfirmPopupController ShowConfirmPopup(
		string message,
		string buttonLabel = "OK",
		GameObject parent = null) {

		PopupController popupController = ShowPopup(Instance.confirmPopupPrefab, message, parent);
		if (popupController == null) {
			return null;
		}

		ConfirmPopupController confirmPopupController = popupController as ConfirmPopupController;
		Assert.IsNotNull(confirmPopupController);

		return confirmPopupController;
	}

	/// <summary>
	/// Shows popup with a text and loading icon in it.
	/// </summary>
	/// <param name="message">Message to be displayed</param>
	/// <param name="parent">
	/// Ui obeject to whick popup will be attached.
	/// If null, then overlay canvas will be automatically searched for
	/// </param>
	/// <returns>Created popup</returns>
	public static PopupController ShowLoadingPopup(string message, GameObject parent = null) {
		return ShowPopup(Instance.loadingPopupPrefab, message, parent);
	}

	public static void CloseActivePopup() {
		if (Instance.ActivePopup != null) {
			Instance.ActivePopup.Close();
		}
	}

	protected override void Awake() {
		base.Awake();

		Assert.IsNotNull(simplePopupPrefab, "Simple popup prefab was not assigned in inspector.");
		Assert.IsNotNull(confirmPopupPrefab, "Confirm popup prefab was not assigned in inspector.");
		Assert.IsNotNull(loadingPopupPrefab, "Loading popup prefab was not assigned in inspector.");
	}

	static PopupController ShowPopup(GameObject prefab, string message, GameObject parent) {
		Assert.IsNotNull(prefab);

		Instance.ActivePopup?.Close();
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

			return null;
		}

		GameObject curPopupObj = Instantiate(prefab, parent.transform);
		Instance.ActivePopup = curPopupObj.GetComponent<PopupController>();
		Assert.IsNotNull(Instance.ActivePopup, "No PopupController on popup instance.");

		Instance.ActivePopup.MessageTextMesh.text = message;
		Instance.ActivePopup.PopupClosing.AddListener(Instance.OnPopupClosing);

		return Instance.ActivePopup;
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
