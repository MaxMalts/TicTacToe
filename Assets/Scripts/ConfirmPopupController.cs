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
	TaskCompletionSource<bool> waitForCloseTask;


	/// <summary>
	/// Wait for confirmation button click or closing of the popup.
	/// </summary>
	/// <returns>
	/// Task which returns: <br/>
	/// <see langword="true"/> if confirmation button clicked <br/>
	/// <see langword="false"/> if closed
	/// </returns>
	public Task<bool> WaitForConfirmOrCloseAsync() {
		//await Task.Run(() => {
		//	lock (closingLock) {
		//		while (!IsClosed) {
		//			Monitor.Wait(closingLock);
		//		}
		//	}
		//});

		if (waitForCloseTask == null) {
			waitForCloseTask = new TaskCompletionSource<bool>();
		}

		return waitForCloseTask.Task;  // will be converted to non-generic Task
	}

	public override void Close() {
		base.Close();

		//lock (closingLock) {
		//	Monitor.PulseAll(closingLock);
		//}

		if (!IsClosed) {
			waitForCloseTask?.SetResult(false);
		}
	}

	public void OnConfirmButtonClicked() {
		if (waitForCloseTask != null && waitForCloseTask.Task.Status != TaskStatus.RanToCompletion) {
			waitForCloseTask.SetResult(true);
		}
	}
}
