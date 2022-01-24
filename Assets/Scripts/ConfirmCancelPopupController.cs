using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.Assertions;



/// <summary>
/// Popup with confirmation and cancellation button (e.g. "Ok" and "Cancel")
/// </summary>
public class ConfirmCancelPopupController : PopupController {

	[SerializeField] TextMeshProUGUI confirmButtonTextMesh;
	public TextMeshProUGUI ConfirmButtonTextMesh {
		get {
			return confirmButtonTextMesh;
		}
	}

	[SerializeField] TextMeshProUGUI cancelButtonTextMesh;
	public TextMeshProUGUI CancelButtonTextMesh {
		get {
			return cancelButtonTextMesh;
		}
	}

	//object closingLock = new object();
	TaskCompletionSource<bool> waitForClickTask;


	/// <summary>
	/// Wait for confirm button click or closing of the popup.
	/// </summary>
	/// <returns>
	/// Task which returns: <br/>
	/// <see langword="true"/> if confirm button was clicked <br/>
	/// <see langword="false"/> if cancel button was clicked or popup was closed
	/// </returns>
	public Task<bool> WaitForConfirmOrCloseAsync() {
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

	public void OnConfirmButtonClicked() {
		waitForClickTask?.TrySetResult(true);
	}

	public void OnCancelButtonClicked() {
		waitForClickTask?.TrySetResult(false);
	}

	protected override void Awake() {
		base.Awake();

		Assert.IsNotNull(confirmButtonTextMesh, "Confirm button Text Mesh was not assigned in inspector.");
		Assert.IsNotNull(cancelButtonTextMesh, "Cancel button Text Mesh was not assigned in inspector.");
	}
}
