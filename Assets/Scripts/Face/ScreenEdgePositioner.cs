using UnityEngine;

public class ScreenEdgePositioner : MonoBehaviour {
    public bool rightCorner = false;
    public bool topCorner = false;
    public float camDistance = 5;
    public int offset = 0;

    // Update is called once per frame
    void Update() {
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(rightCorner ? Screen.width - offset : offset, topCorner ? Screen.height - offset : offset, camDistance));
    }
}
