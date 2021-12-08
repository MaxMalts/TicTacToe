using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;



/// <summary>
/// Used to not search for TextMeshPro through popup children.
/// </summary>
public class PopupTextVar : MonoBehaviour {

	[SerializeField] TextMeshProUGUI messageTextMesh;
	public TextMeshProUGUI MessageTextMesh {
		get {
			return messageTextMesh;
		}
	}


	void Awake() {
		Assert.IsNotNull(MessageTextMesh, "Message Text Mesh not assigned in inspector.");
	}
}
