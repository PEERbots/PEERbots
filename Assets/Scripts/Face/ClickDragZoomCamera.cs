using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClickDragZoomCamera: MonoBehaviour {
 
    public PortraitLandscape portraitLandscape;

    public Transform camera;
    public float dragSpeed = 3;
    private bool clicked = false;

    public Vector2 maxPosition;
    private Vector3 dragOrigin;
    private Vector3 camOrigin;

    public void SetZoom(float zoom) {
        camera.position = new Vector3(camera.position.x,camera.position.y,zoom);
    }

    void Update() {
        //Check if clicked on UI slider
        bool touchOrClickedUI = false;
        if(Application.platform == RuntimePlatform.IPhonePlayer || 
           Application.platform == RuntimePlatform.Android) {
            if(Input.touchCount > 0) {
                Touch touch = Input.GetTouch(0);
                touchOrClickedUI = EventSystem.current.IsPointerOverGameObject(touch.fingerId) || 
                                   EventSystem.current.IsPointerOverGameObject();        
            }
        } else {
            touchOrClickedUI = EventSystem.current.IsPointerOverGameObject();    
        }
        if(!touchOrClickedUI) {
            //Use mouse click to control drag reposition of face.
            if (Input.GetMouseButtonDown(0)) {
                dragOrigin = Input.mousePosition;
                camOrigin = camera.position;
                clicked = true;
                return;
            }
            if (Input.GetMouseButton(0) && clicked) {
                Vector3 pos = Camera.main.ScreenToViewportPoint(Input.mousePosition - dragOrigin);
                Vector3 move = new Vector3(pos.x * dragSpeed, pos.y * dragSpeed, 0);
                //Change move direction if portrait or landscape orientation specified:
                if(portraitLandscape != null) {
                    switch(portraitLandscape.orientation) {
                        case PortraitLandscape.Orientation.LandscapeLeft: 
                            move = new Vector3(pos.x * dragSpeed, pos.y * dragSpeed, 0);
                            break;
                        case PortraitLandscape.Orientation.LandscapeRight: 
                            move = new Vector3(-pos.x * dragSpeed, -pos.y * dragSpeed, 0);
                            break;
                        case PortraitLandscape.Orientation.PortraitLeft: 
                            move = new Vector3(pos.y * dragSpeed, -pos.x * dragSpeed, 0);
                            break;
                        case PortraitLandscape.Orientation.PortraitRight: 
                            move = new Vector3(-pos.y * dragSpeed, pos.x * dragSpeed, 0);
                            break;
                    }
                }
                camera.position = camOrigin - move;
            }
            if(Input.GetMouseButtonUp(0)) {
                clicked = false;
            }
        }
        //Clamp Camera position:
        float x = camera.position.x;
        float y = camera.position.y;
        float z = camera.position.z;
        if(x > maxPosition.x) {x = maxPosition.x;}
        if(x < -maxPosition.x) {x = -maxPosition.x;}
        if(y > maxPosition.y) {y = maxPosition.y;}
        if(y < -maxPosition.y) {y = -maxPosition.y;}
        camera.position = new Vector3(x,y,z);
    }
}