using UnityEngine;

public class ShowHideConsole : MonoBehaviour {

    public GameObject console;
    public bool hideOnStart = true;

    public void Start() {
        if(hideOnStart) { console.SetActive(false); }
    }

    // Update is called once per frame
    void Update() {
        if(console != null) {
            if(Input.GetKeyDown(KeyCode.BackQuote)) {
                console.SetActive(!console.activeSelf);
            }
        }
    }

}
