using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.Assertions;

/// <summary>
/// Popup with confirmation button (e.g. "Ok")
/// </summary>
public class ConfirmPopupController : PopupController {

	//object closingLock = new object();

	// object will be always set to null. There is no non-generic TaskCompletionSource
	TaskCompletionSource<object> waitForCloseTask;


	// Wait for closing of the popup. Pressing the confirmation button also closes the popup.
	public Task WaitForConfirmAndCloseAsync() {
		//await Task.Run(() => {
		//	lock (closingLock) {
		//		while (!IsClosed) {
		//			Monitor.Wait(closingLock);
		//		}
		//	}
		//});

		if (waitForCloseTask == null) {
			waitForCloseTask = new TaskCompletionSource<object>();
		}

		return waitForCloseTask.Task;  // will be converted to non-generic Task
	}

	public override void Close() {
		base.Close();

		//lock (closingLock) {
		//	Monitor.PulseAll(closingLock);
		//}

		if (!IsClosed) {
			waitForCloseTask?.SetResult(null);
		}
	}

	public void OnConfirmButtonClicked() {
		Close();
	}
}
