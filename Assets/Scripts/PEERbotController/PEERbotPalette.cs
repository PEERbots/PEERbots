using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PEERbotPalette : MonoBehaviour {

  
  public string title = "";
  
  public List<PEERbotButton> buttons;

  [System.NonSerialized]
  public int index;

  [System.NonSerialized]
  private PEERbotController wc;

  [System.NonSerialized]
  private PEERbotLogger logger;

  void Awake() { 
    buttons = new List<PEERbotButton>();
    wc = GlobalObjectFinder.FindGameObjectWithTag("PEERbotController").GetComponent<PEERbotController>(); 
    logger = GlobalObjectFinder.FindGameObjectWithTag("PEERbotLogger").GetComponent<PEERbotLogger>(); 
  } 

  public void Select() {
    wc.selectPalette(this);
  }

}

[System.Serializable]
public class PEERbotPaletteData {
  public string title = "";
  public string date;
  public string time;
  public List<PEERbotButtonData> buttons;
}

[System.Serializable]
public class PEERbotPaletteLogData
{
  public string title = "";
  public string date;
  public string time;
}