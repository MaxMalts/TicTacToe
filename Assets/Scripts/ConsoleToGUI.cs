﻿using UnityEngine;

namespace DebugStuff {
	public class ConsoleToGUI : MonoBehaviour {
		//#if !UNITY_EDITOR
		static string myLog = "";
		string output;
		string stack;


		public void Log(string logString, string stackTrace, LogType type) {
			output = logString;
			stack = stackTrace;
			myLog = output + "\n" + myLog;
			if (myLog.Length > 5000) {
				myLog = myLog.Substring(0, 4000);
			}
		}

		void OnEnable() {
			Application.logMessageReceived += Log;
		}

		void OnDisable() {
			Application.logMessageReceived -= Log;
		}

		void OnGUI() {
			//if (!Application.isEditor) //Do not display in editor ( or you can use the UNITY_EDITOR macro to also disable the rest)
			{
				myLog = GUI.TextArea(new Rect(10, 10, Screen.width - 10, Screen.height - 200), myLog);
			}
		}
		//#endif
	}
}