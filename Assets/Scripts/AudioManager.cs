using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



public class AudioManager : Singleton<AudioManager> {

	public class MuteChangedEvent : UnityEvent<bool> { }
	public MuteChangedEvent MuteChanged { get; } = new MuteChangedEvent();

	[SerializeField] bool audioMuted;
	public bool AudioMuted {
		get {
			return audioMuted;
		}
		
		set {
			if (audioMuted != value) {
				audioMuted = value;
				MuteChanged.Invoke(value);
			}
		}
	}
}
