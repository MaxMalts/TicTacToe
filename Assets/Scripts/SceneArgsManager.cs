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

	private static Dictionary<string, object> curArgs = new Dictionary<string, object>(),
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

	private static void OnSceneUnloaded(Scene scene) {
		curArgs.Clear();

		Dictionary<string, object> temp = curArgs;
		curArgs = nextArgs;
		nextArgs = temp;
	}
}
