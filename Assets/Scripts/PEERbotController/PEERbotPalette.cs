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

  void Awake() { 
    buttons = new List<PEERbotButton>();
    wc = GlobalObjectFinder.FindGameObjectWithTag("PEERbotController").GetComponent<PEERbotController>(); 
  } 

  public void Select() {
    wc.selectPalette(this);
  }

}

[System.Serializable]
public class PEERbotPaletteData {
  public string title = "";
  public List<PEERbotButtonData> buttons;
}