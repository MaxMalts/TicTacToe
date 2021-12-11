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
public class ButtonPopupController : PopupController {

	[SerializeField] TextMeshProUGUI buttonTextMesh;
	public TextMeshProUGUI ButtonTextMesh {
		get {
			return buttonTextMesh;
		}
	}

	//object closingLock = new object();
	TaskCompletionSource<bool> waitForClickTask;


	/// <summary>
	/// Wait for button click or closing of the popup.
	/// </summary>
	/// <returns>
	/// Task which returns: <br/>
	/// <see langword="true"/> if button was clicked <br/>
	/// <see langword="false"/> if closed
	/// </returns>
	public Task<bool> WaitForClickOrCloseAsync() {
		//await Task.Run(() => {
		//	lock (closingLock) {
		//		while (!IsClosed) {
		//			Monitor.Wait(closingLock);
		//		}
		//	}
		//});

		if (waitForClickTask == null) {
			waitForClickTask = new TaskCompletionSource<bool>();
		}

		return waitForClickTask.Task;
	}

	public override void Close() {
		if (!IsClosed) {
			base.Close();

			//lock (closingLock) {
			//	Monitor.PulseAll(closingLock);
			//}

			waitForClickTask?.TrySetResult(false);
		}
	}

	public void OnButtonClicked() {
		waitForClickTask?.TrySetResult(true);
	}

	protected override void Awake() {
		base.Awake();

		Assert.IsNotNull(buttonTextMesh, "Button Text Mesh was not assigned in inspector.");
	}
}
