using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



[RequireComponent(typeof(Button))]
public class MainMenuSignButtonController : MonoBehaviour {

	public CellSign buttonSign;

	Button button;


	public void OnButtonClicked() {
		MainMenuAPI.Instance.StartGame(buttonSign);
	}

	void Awake() {
		button = GetComponent<Button>();
		button.onClick.AddListener(OnButtonClicked);
	}
}