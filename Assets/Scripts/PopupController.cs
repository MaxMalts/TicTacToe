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

	bool isClosing = false;


	public virtual void Close() {
		if (!IsClosed && !isClosing) {
			isClosing = true;
			PopupClosing.Invoke();
			Destroy(gameObject);
			IsClosed = true;
		}
	}

	protected virtual void OnDestroy() {
		Close();
	}
}
