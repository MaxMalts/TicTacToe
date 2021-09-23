using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;



public class LoadingPopupAPI : MonoBehaviour {

	public string Message {
		get {
			return textMesh.text;
		}

		set {
			textMesh.text = value;
		}
	}

	[SerializeField] TextMeshProUGUI textMesh;


	void Awake() {
		Assert.IsNotNull(textMesh,
			"TextMeshPro component for message was not assigned in inspector.");
	}
}
