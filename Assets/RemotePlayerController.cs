using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class NetworkManager {

}


public class RemotePlayerController : MonoBehaviour, PlayerController {
	public PlayerAPI Player { get; private set; }

	[SerializeField] private NetworkManager networkManager;
}
