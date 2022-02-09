using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;



[RequireComponent(typeof(TextMeshProUGUI))]
public class StatusTextController : MonoBehaviour {

	TextMeshProUGUI textMesh;
	Animation textChangeAnimation;

	public string Text {
		get {
			return textMesh.text;
		}

		set {
			textMesh.text = value;
		}
	}


	public void SetTextAndAnimate(string text) {
		if (textChangeAnimation != null) {
			textChangeAnimation.Play();
		}

		textMesh.text = text;
	}
	
	void Awake() {
		textMesh = GetComponent<TextMeshProUGUI>();
		Assert.IsNotNull(textMesh, "No TextMeshPro component on StatusTextController.");

		textChangeAnimation = GetComponent<Animation>();
		if (textChangeAnimation == null) {
			Debug.LogWarning("No Animation component on Status Text GameObject to animate text change.");
		}
	}
}
