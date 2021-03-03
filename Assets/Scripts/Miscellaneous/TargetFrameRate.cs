using UnityEngine;

public class TargetFrameRate : MonoBehaviour {
    public int fps = 300;
    void Start() { 
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps; 
    }
}
