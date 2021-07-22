using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Singleton<T> : MonoBehaviour where T : Singleton<T> {

	private static T instance = null;
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

		} else {
			Destroy(gameObject);
		}
	}
}
