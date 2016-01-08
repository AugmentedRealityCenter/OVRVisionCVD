using UnityEngine;
using System.Collections;

public class QuitOnEsc : MonoBehaviour {
	// Update is called once per frame
	void Update () {
		if (Input.GetKey("escape"))
		Application.Quit();
	}
}
