using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using F10.StreamDeckIntegration;

public class PEERbotController : MonoBehaviour {

  public PEERbotButtonEditorUI editorUI;
  public PEERbotSaveLoad saveloader;
  public PEERbotLogger logger;

  [Header("PEERbotPalette Variables")]
  public List<PEERbotPalette> palettes;
  public GameObject paletteCopy;

  [System.NonSerialized] public PEERbotPalette currentPalette = null;
  [System.NonSerialized] public PEERbotButton currentButton = null;
  public PEERbotButton revertButton;

  [Header("Button Variables")]
  public GameObject buttonCopy;

  ///***********************************************///
  ///***********INITIALIZATION FUNCTIONS************///
  ///***********************************************///

	// Registers this class as a StreamDeck enabled class
	private void OnEnable() { StreamDeck.Add(this); }
	private void OnDisable() { StreamDeck.Remove(this); }

  ///***********************************************///
  ///***************PALETTE FUNCTIONS***************///
  ///***********************************************///
  public int getPaletteCount() { 
    if(palettes != null) { return palettes.Count; } else { return -1; }
  }
  public void newPaletteOnClick() { newPalette(); }
  public PEERbotPalette newPalette(bool streamingAsset = false) {
    //Game Object Vars
    GameObject newPaletteObject = Instantiate(paletteCopy, Vector3.zero, Quaternion.identity);
    newPaletteObject.transform.SetParent(paletteCopy.transform.parent, true);
    newPaletteObject.transform.localScale = new Vector3(1,1,1);
    newPaletteObject.SetActive(true);
    //Change color to denote default streaming asset
    if(streamingAsset) { newPaletteObject.transform.Find("TitleText").GetComponent<Text>().color = new Color32(150, 80, 220, 255); }
    //Palette vars
    PEERbotPalette newPalette = newPaletteObject.GetComponent<PEERbotPalette>();
    if(newPalette == null) { Debug.LogError("Palette script on PaletteCopy not found. PaletteCopy must have Palette script attached!"); return null; }

    newPalette.title = "Palette " + (int)Random.Range(0,100);

    selectPalette(newPalette);
    palettes.Add(newPalette);
    return newPalette;
  }
  public void deletePaletteOnClick() { deletePalette(currentPalette); }
  public void deleteAllPalettes() { 
    while(palettes != null && palettes.Count > 0) { deletePalette(palettes[0]); } 
  }
  public void deletePalette(PEERbotPalette palette) { if(palette == null) { Debug.LogWarning("No palette selected! Cannot delete palette."); return; }
    //Delete all current buttons
    foreach(PEERbotButton button in palette.buttons) { Destroy(button.gameObject); }
    //Remove the palette from the list
    palettes.Remove(palette);
    //Destroy the palette
    Destroy(palette.gameObject);
    palette = null;
  }
  public void selectPalette(PEERbotPalette palette) { if(palette == null) { Debug.LogWarning("Null palette! Cannot select palette."); return; }
    //Hide all unselected palette buttons.
    if(currentPalette != null) {
      foreach(PEERbotButton button in currentPalette.buttons) { button.gameObject.SetActive(false); }
      currentPalette.gameObject.transform.Find("SelectedOutline").gameObject.SetActive(false);
    }
    //Show/hide outlines  
    palette.gameObject.transform.Find("SelectedOutline").gameObject.SetActive(true);
    //Show all selected palette buttons.
    foreach(PEERbotButton button in palette.buttons) { 
      button.gameObject.SetActive(true); 
    }
    //Select nothing
    selectButton(null);
    //Set currentPalette
    currentPalette = palette;
    //Change Title Text
    editorUI.setPaletteTitle(palette.title);
    //Debug.Log("Selected Palette!");
  }
  ///**********************************************///
  ///***************BUTTON FUNCTIONS***************///
  ///**********************************************///
  public void newButtonOnClick() {
    if(palettes.Count == 0) { newPalette(); }  
    if(currentPalette == null) { Debug.LogWarning("No Palette selected. Cannot create button."); return; } 
    newButton();
  }
  public PEERbotButton newButton() {
    //Game Object Vars
    GameObject newButtonObject = Instantiate(buttonCopy, Vector3.zero, Quaternion.identity);
    newButtonObject.transform.SetParent(buttonCopy.transform.parent, true);
    newButtonObject.transform.localScale = new Vector3(1,1,1);
    newButtonObject.name = "Button" + (int)Random.Range(0,1000);
    newButtonObject.SetActive(true);
    //Button vars
    PEERbotButton newButton = newButtonObject.GetComponent<PEERbotButton>();
    if(newButton == null) { Debug.LogError("Button script on ButtonCopy not found. PaletteCopy must have Palette script attached!"); return null; }
    //Create a random button title
    //newButton.data.title = newButtonObject.name;
    //Add New Button to current palettes
    currentPalette.buttons.Add(newButton);
    selectButton(newButton);
    return newButton;
  }
  public void selectButton(PEERbotButton button) {
    //Save a copy of the current button
    revertButton.setButtonToTemplate(button);

    //Show/hide outlines    
    if(currentButton != null) currentButton.gameObject.transform.Find("SelectedOutline").gameObject.SetActive(false);
    if(button != null) button.gameObject.transform.Find("SelectedOutline").gameObject.SetActive(true);

    currentButton = button;
    editorUI.setUItoButton(button);
  }
  public void deleteButtonOnClick() { deleteButton(currentButton); }
  public void deleteButton(PEERbotButton button) { if(button == null) { Debug.LogWarning("No button selected! Cannot delete button."); return; }
    //No button selected.    
    currentPalette.buttons.Remove(button);
    Destroy(button.gameObject);
    button = null;
    //saveloader.SaveCurrentCSVPalette(); //Quick save
  }
  public void copyButton() { if(currentButton == null) { Debug.LogWarning("No button selected! Cannot copy button."); return; }
    PEERbotButton prevButton = currentButton;
    PEERbotButton button = newButton();
    button.setButtonToTemplate(prevButton);
    selectButton(button);
    //saveloader.SaveCurrentCSVPalette(); //Quick save
  }
  public void revertButtonOnClick() { if(revertButton == null) { Debug.LogWarning("No revertButton found! Please assign."); return; }
    Debug.Log("Button reverted!");
    editorUI.setUItoButton(revertButton);
    //saveloader.SaveCurrentCSVPalette(); //Quick save
  }

}