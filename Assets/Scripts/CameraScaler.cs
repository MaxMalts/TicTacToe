using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class CameraScaler : MonoBehaviour {

	[SerializeField]
	[Min(0.00001f)]
	[Tooltip("Width which should always be in view.")]
	float targetWidth;
	public float TargetWidth {
		get {
			return targetWidth;
		}

		set {
			if (value < 1) {
				value = 1;
			}
			targetWidth = value;
		}
	}

	[SerializeField]
	[Min(0.00001f)]
	[Tooltip("Height which should always be in view.")]
	float targetHeight;
	public float TargetHeight {
		get {
			return targetHeight;
		}

		set {
			if (value < 1) {
				value = 1;
			}
			targetHeight = value;
		}
	}

	Camera camera;


	void Update() {
		if (camera == null) {
			camera = GetComponent<Camera>();
		}

		if (camera != null) {
			float sizeWidth = targetWidth * Screen.height / Screen.width * 0.5f;
			float sizeHeight = targetHeight / 2;
			camera.orthographicSize = Math.Max(sizeWidth, sizeHeight);
		}
	}

	void Reset() {
		targetWidth = 1;
	}
}
