using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;



public class LoadingPopupTextVar : MonoBehaviour {

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
