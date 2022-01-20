using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;



/// <summary>
/// Used to pass arguments to the new loaded scene.
/// Works only when a signle scene is loaded at a time.
/// </summary>
public static class SceneArgsManager {

	static SceneArgsManager() {
		SceneManager.sceneUnloaded += OnSceneUnloaded;
	}

	static Dictionary<string, object> curArgs = new Dictionary<string, object>(),
		nextArgs = new Dictionary<string, object>();

	public static Dictionary<string, object> CurSceneArgs {
		get {
			return curArgs;
		}
	}

	public static Dictionary<string, object> NextSceneArgs {
		get {
			return nextArgs;
		}

		set {
			nextArgs = value;
		}
	}

	/// <summary>
	/// Get scene argument immediately of needed type.
	/// </summary>
	/// 
	/// <typeparam name="T">Type of argument</typeparam>
	/// <param name="key">Argument name</param>
	/// <param name="value">
	/// Argument value if exists and is of type T, otherwise default value for T
	/// </param>
	/// 
	/// <returns>
	/// <see langword="true"/> if needed argument exists and is of type T,
	/// otherwise <see langword="false"/>
	/// </returns>
	/// <exception cref="System.ArgumentNullException">key was null</exception>
	public static bool TryGetArg<T>(string key, out T value) {
		object resObj;
		CurSceneArgs.TryGetValue(key, out resObj);
		if (resObj == null) {
			value = default;
			return false;
		}

		try {
			value = (T)resObj;
		} catch {
			value = default;
			return false;
		}

		return true;
	}

	static void OnSceneUnloaded(Scene scene) {
		curArgs.Clear();

		Dictionary<string, object> temp = curArgs;
		curArgs = nextArgs;
		nextArgs = temp;
	}
}
