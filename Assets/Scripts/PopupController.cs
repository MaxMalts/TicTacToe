using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.Assertions;



/// <summary>
/// Controller for standard popups. Inherit this class to define custom popups.
/// </summary>
public class PopupController : MonoBehaviour {

	public UnityEvent PopupClosing { get; } = new UnityEvent();

	[SerializeField] TextMeshProUGUI messageTextMesh;
	public TextMeshProUGUI MessageTextMesh {
		get {
			return messageTextMesh;
		}
	}

	public bool IsClosed { get; private set; } = false;



	/// <summary>
	/// If you override this method, place base.Close() in the beginning and
	/// check for IsClosed as in this method to avoid multiple closing.
	/// </summary>
	public virtual void Close() {
		if (!IsClosed) {
			IsClosed = true;
			PopupClosing.Invoke();
			Destroy(gameObject);
		}
	}

	protected virtual void Awake() {
		Assert.IsNotNull(messageTextMesh, "Message Text Mesh was not assigned in inspector.");
	}

	protected virtual void OnDestroy() {
		Close();
	}
}
