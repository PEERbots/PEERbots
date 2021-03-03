using UnityEngine;
using UnityEngine.UI;

public class TankController : MonoBehaviour {

  public SendMode sendMode;

  public SimpleInputNamespace.Joystick joystick;
  public HoldButton rightRotateButton;
  public HoldButton leftRotateButton;
  public GameObject joystickUI;
  
  public GameObject serialPortObject;
  public UDPSender udpSender;
  public Misty misty;

  public float remoteReceiveTimer = 0;
  public float remoteReceiveTime = 1;
  public float remoteX, remoteY = 0;

  public float serialTimer = 2f;
  public float serialTime = 0.3f;

  public int maxSpeed = 60; //0-100
  public Slider maxSpeedSlider;
  private bool stopped = false;
  private float stopCounter = 0;
  
  //Get Max Speed Value Preference
  public void Start() {
    if(maxSpeedSlider) { 
      maxSpeed = PlayerPrefs.GetInt("MaxSpeed", 60);
      maxSpeedSlider.value = maxSpeed; 
    }
  }
  public void SetMaxSpeed(float speed) { 
    if(speed < 0) { speed = 0; } if(speed > 100) { speed = 100; }
    maxSpeed = (int)speed; PlayerPrefs.SetInt("MaxSpeed", maxSpeed);
  }

  public void Update() {
    //See if remote control request has been made yet:
    if(remoteReceiveTimer > 0) { remoteReceiveTimer -= Time.deltaTime; }

    //Check if left or right turn buttons are pressed:
    float horizontal, vertical = 0;
    horizontal = joystick.GetX(); vertical = joystick.GetY();
    if(leftRotateButton != null && leftRotateButton.pressed) { horizontal = -1; vertical = 0; } 
    else if (rightRotateButton != null && rightRotateButton.pressed) { horizontal = 1; vertical = 0; } 
    
    //Update tank every time serial is ready to push another message
    if(serialTimer > 0) { serialTimer -= Time.deltaTime; }
    else { serialTimer = serialTime; 
      //If USB Serial, let incoming Bluetooth/UDP messages override joystick
      /* if(sendMode == SendMode.SERIAL) {
        if(remoteReceiveTimer > 0) { controlTank(remoteX, remoteY); } 
        else { controlTank(horizontal, vertical); }
      } 
      //If Bluetooth or UDP, send data from Joystick to face device.
      else if(sendMode == SendMode.BLUETOOTH || sendMode == SendMode.UDP) {
        controlTank(horizontal, vertical); 
      } */
      //If Misty, send data from Joystick to Misty device.
      if(sendMode == SendMode.MISTY) {
        if(leftRotateButton.pressed || rightRotateButton.pressed) {
          //misty.DriveTrack((int)(vertical*maxSpeed) - (int)(horizontal*maxSpeed), (int)(vertical*maxSpeed) + (int)(horizontal*maxSpeed));
          //misty.DriveTime((int)(vertical*maxSpeed), -(int)(horizontal*maxSpeed), (int)(serialTime*2000));
          misty.Drive((int)(vertical*maxSpeed), -(int)(horizontal*maxSpeed));
          stopped = false;
        } else if(new Vector2(joystick.GetX(),joystick.GetY()).magnitude > 0.05f) {
          //misty.DriveTrack((int)(vertical*maxSpeed) - (int)(horizontal*maxSpeed), (int)(vertical*maxSpeed) + (int)(horizontal*maxSpeed));
          //misty.DriveTime((int)(vertical*maxSpeed), -(int)(horizontal*maxSpeed), (int)(serialTime*2000));
          misty.Drive((int)(vertical*maxSpeed), -(int)(horizontal*maxSpeed));
          stopped = false;
        } else {
          //misty.Halt();
          //Stop misty immediately
          if(!stopped) { 
            misty.Drive(0,0);
            misty.Stop(); 
            misty.DisableHazardSensors(); //also disable hazard sensors
            stopped = true; 
          }
          //If for whatever reason Misty didn't stop, stop again every x counts to be safe.
          stopCounter++;
          if(stopCounter == 30) { 
            stopCounter = 0; 
            misty.Drive(0,0);
            misty.Stop(); 
            misty.DisableHazardSensors(); //also disable hazard sensors
          }
        }
      }
    }
  }
  
  public void setSendMode(int mode) {
    switch(mode) {
      case 0: sendMode = SendMode.NONE; break;
      case 1: sendMode = SendMode.UDP; break;
      case 2: sendMode = SendMode.MISTY; break;
      case 3: sendMode = SendMode.BLUETOOTH; break;
      case 4: sendMode = SendMode.SERIAL; if(serialPortObject) { serialPortObject.SetActive(true); } break;
      default: sendMode = SendMode.NONE; break;
    }
  }

  public void remoteControlTank(float horizontal, float vertical) {
    remoteReceiveTimer = remoteReceiveTime;
    remoteX = horizontal; remoteY = vertical;
  }

  /* public void controlTank(float horizontal, float vertical) {
    JSONObject json = new JSONObject();
    if(sendMode == SendMode.SERIAL) {
      int left = (int)((vertical + horizontal) * 255f * ((float)maxSpeed/100f) );
      int right = (int)((vertical - horizontal) * 255f * ((float)maxSpeed/100f) );
      if(left > 255) { left = 255; } if(left < -255) { left = -255; }
      if(right > 255) { right = 255; } if(right < -255) { right = -255; }
      json.AddField("L", left); json.AddField("R", right);
    } else {
      json.AddField("x", (int)(horizontal*(float)maxSpeed) ); json.AddField("y", (int)(vertical*(float)maxSpeed) );
    }
  } */

  public void showHideJoystick() { joystickUI.SetActive(!joystickUI.activeSelf); } 
}


