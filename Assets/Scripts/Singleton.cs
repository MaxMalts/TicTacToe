using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



/// <summary>
/// Implements the singleton pattern.<br/>
/// You should ingerit from this class to get a singleton class.
/// </summary>
/// 
/// <typeparam name="T">
/// Class which must be singleton
/// and which is ingerited from this class.
/// </typeparam>
public class Singleton<T> : MonoBehaviour where T : Singleton<T> {

	static T instance = null;
	public static T Instance {
		get {
			if (instance == null) {
				instance = FindObjectOfType<T>();

				if (instance == null) {
					GameObject newObject = new GameObject();
					newObject.name = typeof(T).Name;
					instance = newObject.AddComponent<T>();
				}
			}

			return instance;
		}
	}

	protected virtual void Awake() {
		if (instance == null) {
			instance = this as T;
			Assert.IsNotNull(instance);
			DontDestroyOnLoad(gameObject);

		} else if (instance != this as T) {
			Destroy(gameObject);
		}
	}
}
