﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class MainMenuSignButtonController : MonoBehaviour {

	public CellSign buttonSign;


	public void OnButtonClicked() {
		MainMenuAPI.Instance.SetSignAndStart(buttonSign);
	}
}