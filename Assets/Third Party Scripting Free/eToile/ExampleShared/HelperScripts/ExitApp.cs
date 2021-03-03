using UnityEngine;
using System.Collections;

public class ExitApp : MonoBehaviour {

	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
            print("Quit app");
        }
    }
}
