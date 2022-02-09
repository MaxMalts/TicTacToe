using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;



[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public class MuteButtonController : MonoBehaviour {

	[SerializeField] Sprite mutedIcon;
	[SerializeField] Sprite unmutedIcon;

	[SerializeField] AudioSource clickAudioSource;

	Image buttonImage;


	public void OnClick() {
		// After this line OnMuteChanged will be called from AudioManager.MuteChanged event
		AudioManager.Instance.AudioMuted ^= true;

		if (!AudioManager.Instance.AudioMuted && clickAudioSource != null) {
			clickAudioSource.Play();
		}
	}

	void Awake() {
		Assert.IsNotNull(mutedIcon, "mutedIcon was not assigned in inspector.");
		Assert.IsNotNull(unmutedIcon, "unmutedIcon was not assigned in inspector.");

		buttonImage = GetComponent<Image>();
		Assert.IsNotNull(buttonImage, "No Image component on MuteButtonController.");

		AudioManager.Instance.MuteChanged.AddListener(OnMuteChanged);
	}

	void Start() {
		if (AudioManager.Instance.AudioMuted) {
			buttonImage.sprite = mutedIcon;
		} else {
			buttonImage.sprite = unmutedIcon;
		}
	}

	void OnMuteChanged(bool muted) {
		// This method is called while inside OnClick

		if (muted) {
			Assert.IsTrue(buttonImage.sprite == unmutedIcon, "Mute button and previous mute state mismatch.");
			buttonImage.sprite = mutedIcon;

		} else {
			Assert.IsTrue(buttonImage.sprite == mutedIcon, "Mute button and previous mute state mismatch.");
			buttonImage.sprite = unmutedIcon;
		}
	}
}
