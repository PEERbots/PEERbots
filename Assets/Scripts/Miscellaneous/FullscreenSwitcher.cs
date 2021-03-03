using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullscreenSwitcher : MonoBehaviour {
    public void ToggleFullScreen() { 
        //If windowed, set to desktop native resolution
        if(!Screen.fullScreen) {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
        } 
        //Otherwise, undo full screen
        else { Screen.fullScreen = !Screen.fullScreen; }
    }
}
