using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PEERbotButton : MonoBehaviour {

  public PEERbotButtonDataFull data;

  [System.NonSerialized]
  private PEERbotController pbc;

  void Awake() { 
    pbc = GlobalObjectFinder.FindGameObjectWithTag("PEERbotController").GetComponent<PEERbotController>(); 
  } 

  public void Select() {
    pbc.selectButton(this);
  }

  public void setButtonToTemplate(PEERbotButton template) {
    if(template == null) { Debug.LogWarning("Template is null! Cannot set button." ); return; }
    JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), this);
  }
  
}

[System.Serializable]
public class PEERbotButtonDataFull : PEERbotButtonData { //Additional data is logged and sent.
  //Logging Vars
  public string date = "";
  public string time = "";
  public string palette = "";
  //Button Vars
  public int r = 0;
  public int g = 255;
  public int b = 255;
  //Speech vars
  public float volume = 1;
  public float rate = 1;
  public float pitch = 1;
  //Gaze Vars
  public float gazeX = 0;
  public float gazeY = 0;
}

[System.Serializable]
public class PEERbotButtonData { //Simplified data is used only for saving/loading.
  //Button Vars
  public string title = "";
  public string color = "";
  //Emotion vars
  public string emotion = "";
  //Speech vars
  public string speech = "";
  //Classification vars
  public string goal = "";
  public string subgoal = "";
  public string proficiency = "";
}

[System.Serializable]
public class PEERbotButtonQuickSpeechData {
  public string logType = "";
  public string speech = "";
  public string palette = "";
  public string date = "";
  public string time = "";
}