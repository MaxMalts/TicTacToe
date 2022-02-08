using System.Collections;
using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// Put this script on every GameObject with AudioSource to disable
/// this AudioSource when sounds in game should be muted.
/// You can use AudioSource normally, except changin enabled property.
/// If AudioSource was disabled on mute, it will stay disabled on unmute.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[AddComponentMenu("Audio/Audio Source Disabler")]
public class AudioSourceDisabler : MonoBehaviour {

	AudioSource audioSource;
	bool audioSourceActuallyEnabled;


	void Awake() {
		audioSource = GetComponent<AudioSource>();

		if (audioSource == null) {
			Debug.LogWarning("No AudioSource on GameObject with AudioSourceDisabler.");

		} else {
			if (AudioManager.Instance.AudioMuted) {
				audioSourceActuallyEnabled = audioSource.enabled;
				audioSource.enabled = false;
			}

			AudioManager.Instance.MuteChanged.AddListener(OnMuteChanged);
		}
	}

	void Update() {
		if (AudioManager.Instance.AudioMuted && audioSource.enabled) {
			audioSource.enabled = false;
		}
	}

	void OnMuteChanged(bool muted) {
		if (muted) {
			audioSourceActuallyEnabled = audioSource.enabled;
			audioSource.enabled = false;

		} else {
			audioSource.enabled = audioSourceActuallyEnabled;
		}
	}
}
