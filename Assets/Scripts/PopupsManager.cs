using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;



public class PopupsManager : Singleton<PopupsManager> {

	public PopupController ActivePopup { get; private set; }

	[SerializeField] bool dimmerBackground = true;
	public bool DimmerBgd {
		get {
			return dimmerBackground;
		}

		set {
			if (bgdImage != null) {
				bgdImage.enabled = value;
			}
			dimmerBackground = value;
		}
	}

	[SerializeField] bool backgroundRaycastTarget = true;
	/// <summary>
	/// Block all iteractions except the popup. DimmerBgd need to be set to
	/// <see langword="true"/>, otherwise ignored.
	/// </summary>
	public bool BgdRaycastTarget {
		get {
			return backgroundRaycastTarget;
		}

		set {
			if (bgdImage != null) {
				bgdImage.raycastTarget = value;
			}
			backgroundRaycastTarget = value;
		}
	}

	[SerializeField] GameObject simplePopupPrefab;
	[SerializeField] GameObject confirmPopupPrefab;
	[SerializeField] GameObject confirmCancelPopupPrefab;
	[SerializeField] GameObject loadingPopupPrefab;
	[SerializeField] GameObject loadingCancelPopupPrefab;

	readonly Color bgdColor = new Color(0.0f, 0.0f, 0.0f, 0.2f);
	Image bgdImage;


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
	public static ButtonPopupController ShowConfirmPopup(
		string message,
		string buttonLabel = "OK",
		GameObject parent = null) {

		return ShowButtonPopup(Instance.confirmPopupPrefab, message, buttonLabel, parent);
	}

	/// <summary>
	/// Shows popup with a text and confirmation button and cancellation in it.
	/// </summary>
	/// <param name="message">Message to be displayed</param>
	/// <param name="buttonLabel">Text inside the confirmation button</param>
	/// <param name="parent">
	/// Ui obeject to whick popup will be attached.
	/// If null, then overlay canvas will be automatically searched for
	/// </param>
	/// <returns>Created popup</returns>
	public static ConfirmCancelPopupController ShowConfirmCancelPopup(
		string message,
		string confirmButtonLabel = "OK",
		string cancelButtonLabel = "Cancel",
		GameObject parent = null) {

		return ShowConfirmCancelPopup(
			Instance.confirmCancelPopupPrefab,
			message,
			confirmButtonLabel,
			cancelButtonLabel,
			parent
		);
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

	/// <summary>
	/// Shows popup with a text, loading icon and cancel button in it.
	/// </summary>
	/// <param name="message">Message to be displayed</param>
	/// <param name="buttonLabel">Text inside the confirmation button</param>
	/// <param name="parent">
	/// Ui obeject to whick popup will be attached.
	/// If null, then overlay canvas will be automatically searched for
	/// </param>
	/// <returns>Created popup</returns>
	public static ButtonPopupController ShowLoadingCancelPopup(
		string message,
		string buttonLabel = "Cancel",
		GameObject parent = null) {

		return ShowButtonPopup(Instance.loadingCancelPopupPrefab, message, buttonLabel, parent);
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
		Assert.IsNotNull(loadingCancelPopupPrefab, "Loading cancel popup prefab was not assigned in inspector.");
	}

	static PopupController ShowPopup(GameObject prefab, string message, GameObject parent) {
		Assert.IsNotNull(prefab);

		Instance.ActivePopup?.Close();
		Assert.IsNull(Instance.ActivePopup, "Closed popup but ActivePopup was not set to null.");

		GameObject actualParent = SearchForParentForPopup(parent);
		if (actualParent == null) {
			return null;
		}

		InitializeBgdImage(actualParent);

		GameObject curPopupObj = Instantiate(prefab, Instance.bgdImage.gameObject.transform);
		Instance.ActivePopup = curPopupObj.GetComponent<PopupController>();
		Assert.IsNotNull(Instance.ActivePopup, "No PopupController on popup instance.");

		Instance.ActivePopup.MessageTextMesh.text = message;
		Instance.ActivePopup.PopupClosing.AddListener(Instance.OnPopupClosing);

		return Instance.ActivePopup;
	}

	static ButtonPopupController ShowButtonPopup(
		GameObject prefab,
		string message,
		string buttonLabel,
		GameObject parent
	) {

		PopupController popupController = ShowPopup(prefab, message, parent);
		if (popupController == null) {
			return null;
		}

		ButtonPopupController buttonPopupController = popupController as ButtonPopupController;
		Assert.IsNotNull(buttonPopupController);

		buttonPopupController.ButtonTextMesh.text = buttonLabel;
		return buttonPopupController;
	}

	static ConfirmCancelPopupController ShowConfirmCancelPopup(
		GameObject prefab,
		string message,
		string confirmButtonLabel,
		string cancelButtonLabel,
		GameObject parent) {

		PopupController popupController = ShowPopup(prefab, message, parent);
		if (popupController == null) {
			return null;
		}

		ConfirmCancelPopupController confirmCancelPopupController =
			popupController as ConfirmCancelPopupController;
		Assert.IsNotNull(confirmCancelPopupController);

		confirmCancelPopupController.ConfirmButtonTextMesh.text = confirmButtonLabel;
		confirmCancelPopupController.CancelButtonTextMesh.text = cancelButtonLabel;
		return confirmCancelPopupController;
	}

	static GameObject SearchForParentForPopup(GameObject preferred = null) {
		if (preferred != null && preferred.GetComponent<RectTransform>() == null) {
			Debug.LogWarning("Parent of popup must be a UI object. " +
				"Overlay canvas will be searched for.");

			preferred = null;
		}

		if (preferred == null) {
			Canvas canvas = FindOverlayCanvas();
			if (canvas != null) {
				preferred = canvas.gameObject;
			}
		}

		if (preferred == null) {
			Debug.LogWarning("There is no UI GameObject to which popup can be " +
				"attached (no UI parent is provided and no overlay canvas was detected. " +
				"Popup will not be shown");

			return null;
		}

		return preferred;
	}

	static void InitializeBgdImage(GameObject parent) {
		Assert.IsNotNull(parent);

		if (Instance.bgdImage != null && Instance.bgdImage.gameObject.transform.parent != parent.transform) {
			Destroy(Instance.bgdImage.gameObject);
			Instance.bgdImage = null;
		}

		if (Instance.bgdImage == null) {
			GameObject bgdImageObj = new GameObject("PopupBackground", typeof(Image));
			Instance.bgdImage = bgdImageObj.GetComponent<Image>();
			Assert.IsNotNull(Instance.bgdImage);

			bgdImageObj.transform.SetParent(parent.transform);
			RectTransform rectTrans = bgdImageObj.GetComponent<RectTransform>();
			Assert.IsNotNull(rectTrans, "No RectTransform on created bgdImageObj");

			rectTrans.anchorMin = new Vector2(0.0f, 0.0f);
			rectTrans.anchorMax = new Vector2(1.0f, 1.0f);
			rectTrans.pivot = new Vector2(0.5f, 0.5f);
			rectTrans.anchoredPosition = new Vector2(0.0f, 0.0f);
			rectTrans.sizeDelta = new Vector2(0.0f, 0.0f);
			rectTrans.localScale = new Vector3(1.0f, 1.0f, 1.0f);

			Instance.bgdImage.enabled = Instance.DimmerBgd;
			Instance.bgdImage.color = Instance.bgdColor;
			Instance.bgdImage.raycastTarget = Instance.BgdRaycastTarget;
		}

		Instance.bgdImage.gameObject.SetActive(true);
	}

	void OnPopupClosing() {
		Assert.IsNotNull(Instance.ActivePopup, "Popup closed but ActivePopup was null in PopupsManager.");
		Instance.bgdImage.gameObject.SetActive(false);
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
