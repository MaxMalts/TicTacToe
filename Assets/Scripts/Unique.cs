using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



/// <summary>
/// If an element is Unique, only one instance of
/// it can be enabled in the scene at the same time.
/// </summary>
/// 
/// <typeparam name="T">
/// Class which must be unique
/// and which is ingerited from this class.
/// </typeparam>
/// 
/// <remarks>
/// If you need to implement the OnEnable or OnDisable methods in the
/// inherited class, make sure you call base.OnEnable or base.OnDisable,
/// at the begining of the overriden methods.
/// </remarks>
public class Unique<T> : MonoBehaviour where T : Unique<T> {

	public static T Instance { get; private set; }


	protected void OnEnable() {
		if (Instance != null) {
			Debug.LogError("Multiple instances of Unique class were enabled. " + 
				"Current instance will be disabled.");

			Instance.enabled = false;
		}

		Instance = this as T;
		Assert.IsNotNull(Instance);
	}

	protected void OnDisable() {
		if (Instance == this) {
			Instance = null;
		}
	}
}
