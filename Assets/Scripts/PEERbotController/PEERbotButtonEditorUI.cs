using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;

public class PEERbotButtonEditorUI : MonoBehaviour {
    public PEERbotController pc;
    public PEERbotMappings mappings;
    public PEERbotSaveLoad saveloader;
    
    [Header("Palette UI")]
    public InputField paletteTitle;

    [Header("Button UI")]
    public InputField buttonTitle;
    public Dropdown buttonColor;

    [Header("Speech UI")]
    public InputField speechField;
    public Slider rateSlider;
    public Slider pitchSlider;
    public Slider volumeSlider;

    [Header("Emotion UI")]
    public Dropdown emotionDropdown;

    [Header("Gaze UI")]
    public Slider gazeXSlider;
    public Slider gazeYSlider;

    [Header("Gaze UI")]
    public Dropdown buttonGoal;
    public Dropdown buttonSubgoalInstantiator;
    public Dropdown buttonProficiency;

    [Header("FileIO UI")]
    public GameObject sharePaletteButton;
    public GameObject exportLogButton;

    ///***********************************************///
    ///***********INITIALIZATION FUNCTIONS************///
    ///***********************************************///
    //Initializing Palettes.
    void Awake() {  
        //Check if anything is null
        if(!paletteTitle)      { Debug.LogWarning("paletteTitle is null!"); }
        if(!buttonTitle)      { Debug.LogWarning("buttonTitle is null!"); }
        if(!buttonColor)      { Debug.LogWarning("buttonColor is null!"); }

        //Hide/Show OS specific buttons
        exportLogButton.SetActive(Application.platform == RuntimePlatform.Android ||
                                  Application.platform == RuntimePlatform.IPhonePlayer);
        sharePaletteButton.SetActive(Application.platform == RuntimePlatform.Android ||
                                     Application.platform == RuntimePlatform.IPhonePlayer);
                                     
        //Init Dropdowns
        initColorDropdown();
        initEmotionDropdown();
        initGoalDropdown();
        initSubgoalDropdown();
    }

    //Initializing Colors.
    public void initColorDropdown() {
        buttonColor.ClearOptions();
        List<string> colorOptions = new List<string>();
        foreach (ColorMap colorMap in mappings.colors) { colorOptions.Add(colorMap.name); }
        buttonColor.AddOptions(colorOptions);
        if (colorOptions.Count > 0) { buttonColor.value = 0; }
        else { Debug.LogWarning("No colors set! Please init colors."); }
    }
    //Initializing Emotions.
    public void initEmotionDropdown() {
        List<string> defaultEmotions = new List<string>();
        foreach(EmotionMap emotion in mappings.emotions) { defaultEmotions.Add(emotion.defaultEmotion); }
        emotionDropdown.ClearOptions();
        emotionDropdown.AddOptions(defaultEmotions);
        if (emotionDropdown.options.Count > 0) { emotionDropdown.value = 0; }
        else { Debug.LogWarning("No emotions set! Please init emotions."); }
    }
    //Initializing Goal
    public void initGoalDropdown() {
        buttonGoal.ClearOptions();
        List<string> goalOptions = new List<string>();
        foreach (GoalMap goalmap in mappings.goals) { goalOptions.Add(goalmap.goal); }
        buttonGoal.AddOptions(goalOptions);
        if (goalOptions.Count > 0) { buttonGoal.value = 0; }
        else { Debug.LogWarning("No goals set! Please init goals."); }
    }
    //Initializing Subgoals
    public void initSubgoalDropdown() {    
        buttonSubgoalInstantiator.gameObject.SetActive(false);
        foreach(GoalMap goalmap in mappings.goals) {
            goalmap.subgoalDropdown = Instantiate(buttonSubgoalInstantiator, Vector3.zero, Quaternion.identity);
            goalmap.subgoalDropdown.transform.SetParent(buttonSubgoalInstantiator.transform.parent, true);
            goalmap.subgoalDropdown.transform.localScale = new Vector3(1,1,1);
            goalmap.subgoalDropdown.GetComponent<RectTransform>().anchoredPosition = buttonSubgoalInstantiator.GetComponent<RectTransform>().anchoredPosition;
            goalmap.subgoalDropdown.gameObject.SetActive(false);
            goalmap.subgoalDropdown.gameObject.name = "Button Subgoal Dropdown (" + goalmap.goal + ")";
            goalmap.subgoalDropdown.ClearOptions();
            goalmap.subgoalDropdown.AddOptions(goalmap.subgoals); 
        }
        switchSubgoalDropdown(0,0);
    }
    //Switch Subgoals
    public void switchSubgoalDropdown() { if(pc.currentButton == null) { Debug.LogWarning("No button selected! Cannot set button subgoal."); }
        int subgoalIndex = getButtonSubgoalIndex(buttonGoal.value, pc.currentButton.data.subgoal);
        switchSubgoalDropdown(buttonGoal.value, subgoalIndex);
    }
    public void switchSubgoalDropdown(int goal, int subgoal) {
        if(0 <= goal && goal < mappings.goals.Count) {
            foreach(GoalMap goalmap in mappings.goals) {
                goalmap.subgoalDropdown.gameObject.SetActive(false);
            }
            mappings.goals[goal].subgoalDropdown.gameObject.SetActive(true);
            mappings.goals[goal].subgoalDropdown.value = (mappings.goals[goal].subgoals.Count>0) ? 1 : 0;
            mappings.goals[goal].subgoalDropdown.value = (0 <= subgoal && subgoal < mappings.goals[goal].subgoals.Count) ? subgoal : 0;
        } else { Debug.LogWarning("Goal [" + goal + "] out of range when switching subgoal dropdown!"); }
    }

    ///*************************************************///
    ///***************BUTTON UI FUNCTIONS***************///
    ///*************************************************///
    
    //This is for all buttons
    public void setUItoButton(PEERbotButton button) {
        //Null button. Reset UI:
        if(button == null) { //Debug.Log("Deselected Button!");        
            //Button
            if(buttonTitle) buttonTitle.text = "";
            if(buttonColor) buttonColor.value = 0;
            //Speech
            if(speechField) speechField.text = "";
            if(rateSlider) rateSlider.value = 1;
            if(pitchSlider) pitchSlider.value = 1;
            if(volumeSlider) volumeSlider.value = 1;
            //Gaze
            if(gazeXSlider) gazeXSlider.value = 0;
            if(gazeYSlider) gazeYSlider.value = 0;
            //Emotion
            if(emotionDropdown) emotionDropdown.value = 0;
        } 
        //Set UI to button:
        else { //Debug.Log("Selected Button!");
            //Speech
            setSpeech(button.data.speech);
            setSpeechVolume(volumeSlider.value); //We don't want this to change when selecting a button
            setSpeechPitch(pitchSlider.value); //We don't want this to change when selecting a button
            setSpeechRate(rateSlider.value); //We don't want this to change when selecting a button
            //Gaze
            setGazeX(gazeXSlider.value); //We don't want this to change when selecting a button
            setGazeY(gazeYSlider.value); //We don't want this to change when selecting a button
            //Emotion
            setEmotion(button.data.emotion);
            //Goal/subgoal/proficiency
            setButtonGoal(button.data.goal);
            setButtonSubgoal(buttonGoal.value, button.data.subgoal);
            setButtonProficiency(button.data.proficiency);
            //Button (must happen last to not overwrite speech)
            setButtonTitle(button.data.title);
            setButtonColor(button.data.color);
        }
    }

    //For Palette Only
    public void setPaletteTitle(string text) { if(!pc.currentPalette) { Debug.LogWarning("No palette selected! Cannot set palette title."); return; }
        text = PEERbotSaveLoad.SanitizeFilename(text); // remove all stupid characters
        if(string.IsNullOrEmpty(text)) { //make sure title is actually something 
            Debug.LogWarning("Palette Title must not be null or empty! Alphanumeric only!");
            text = "Palette " + (int)UnityEngine.Random.Range(0,100);
        }
        string previousName = pc.currentPalette.title; //get previousname
        pc.currentPalette.title = text; //Set data
        paletteTitle.text = text; //Set UI
        if(!string.IsNullOrEmpty(text)) pc.currentPalette.gameObject.name = text; //Change GameObject Name
        //Change Title Text
        GameObject titleTextObj = pc.currentPalette.gameObject.transform.Find("TitleText").gameObject;
        if(titleTextObj && titleTextObj.GetComponent<Text>()) { titleTextObj.GetComponent<Text>().text = text; }
        else { Debug.LogWarning("Palette object must have TitleText child!"); }
        //Delete Previous File and Save new one
        if(previousName != pc.currentPalette.title) {
            saveloader.DeleteJSONPaletteFileWithName(previousName);
            saveloader.SaveCurrentJSONPalette();
        }
    }

    //For Button Color and Title
    public void setButtonTitle(string text) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set button title."); return; }
        buttonTitle.text = text; //Set UI
        pc.currentButton.data.title = text; //Set data
        if(!string.IsNullOrEmpty(text)) pc.currentButton.gameObject.name = text; //Change GameObject Name
        //Change Title Text
        GameObject titleTextObj = pc.currentButton.gameObject.transform.Find("TitleText").gameObject;
        if(titleTextObj && titleTextObj.GetComponent<Text>()) { 
        titleTextObj.GetComponent<Text>().text = text; }
        else { Debug.LogWarning("Button object must have TitleText child!"); }
        //If button speech data is empty (and title is not), replace it with title
        if((!string.IsNullOrEmpty(text)) && string.IsNullOrEmpty(pc.currentButton.data.speech)) { setSpeech(text); }
    }
    public Color32 getButtonColor32() { return mappings.colors[buttonColor.value].color; }
    
    public void setButtonColor(string text) {
        setButtonColor(mappings.getColorIndexFromString(text));
    }
    public void setButtonColor(int index) {  if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set button color."); return; }
        buttonColor.value = index; //Set UI    
        pc.currentButton.data.color = mappings.colors[index].name; //Set Data
        pc.currentButton.data.r = mappings.colors[index].color.r; //Set Data
        pc.currentButton.data.g = mappings.colors[index].color.g; //Set Data
        pc.currentButton.data.b = mappings.colors[index].color.b; //Set Data
        pc.currentButton.gameObject.GetComponent<Image>().color = mappings.colors[index].color; //Change Button Color
    }
    
    //Speech vars
    public void setSpeech(string text) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set speech."); return; }
        if(speechField) speechField.text = text; //Set UI
        pc.currentButton.data.speech = text; //Set data
    }
    public void setSpeechVolume(float value) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set volume."); return; }
        if(volumeSlider) volumeSlider.value = value; //Set UI
        pc.currentButton.data.volume = value; //Set data
    }
    public void setSpeechPitch(float value) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set pitch."); return; }
        if(pitchSlider) pitchSlider.value = value; //Set UI
        pc.currentButton.data.pitch = value; //Set data
    }
    public void setSpeechRate(float value) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set rate."); return; }
        if(rateSlider) rateSlider.value = value; //Set UI
        pc.currentButton.data.rate = value; //Set data
    }
    //Gaze Vars
    public void setGazeX(float value) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set gazeX."); return; }
        if(gazeXSlider) gazeXSlider.value = value; //Set UI
        pc.currentButton.data.gazeX = value; //Set data
    }
    public void setGazeY(float value) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set gazeY."); return; }
        if(gazeYSlider) gazeYSlider.value = value; //Set UI
        pc.currentButton.data.gazeY = value; //Set data
    }
    //Emotion vars
    public void setEmotion(string text) {
        setEmotion(mappings.getEmotionIndexFromString(text));
    }
    public void setEmotion(int index) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set emotion."); return; }    
        pc.currentButton.data.emotion = mappings.emotions[index].defaultEmotion; //Set Data
        emotionDropdown.value = index; //Set UI    
    }
    //Goal/Subgoal/Proficiency vars    
    public void setButtonGoal(string goal) { 
        for(int i = 0; i < mappings.goals.Count; i++) { 
            if (goal.ToLower() == mappings.goals[i].goal.ToLower()) { setButtonGoal(i); return; } 
        }
    }
    public void setButtonGoal(int index) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set goal."); return; }        
        pc.currentButton.data.goal = mappings.goals[index].goal; 
        buttonGoal.value = index;
    }
    public int getButtonSubgoal() { return mappings.goals[buttonGoal.value].subgoalDropdown.value; }
    public void setButtonSubgoal(int index) { setButtonSubgoal(buttonGoal.value, index); }
    public void setButtonSubgoal(string goal, string subgoal) { 
        for(int i = 0; i < mappings.goals.Count; i++) { 
            if (goal.ToLower() == mappings.goals[i].goal.ToLower()) { setButtonSubgoal(i, subgoal); return; } 
        }
    }
    public int getButtonSubgoalIndex(int i, string subgoal) { 
        for(int j = 0; j < mappings.goals[i].subgoals.Count; j++) { 
            if (subgoal.ToLower() == mappings.goals[i].subgoals[j].ToLower()) { return j; } 
        }
        return 0;
    }
    public void setButtonSubgoal(int i, string subgoal) { if(!(0 <= i && i < mappings.goals.Count)) { return ; }
        for(int j = 0; j < mappings.goals[i].subgoals.Count; j++) { 
            if (subgoal.ToLower() == mappings.goals[i].subgoals[j].ToLower()) { setButtonSubgoal(i, j); return; } 
        }
    }
    public void setButtonSubgoal(int i, int j) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set subgoal."); return; }
        mappings.goals[i].subgoalDropdown.value = j; //UI
        string subgoal = mappings.goals[i].subgoalDropdown.options[j].text;
        pc.currentButton.data.subgoal = subgoal;
    }
    public void setButtonProficiency(string proficiency) { 
        for(int i = 0; i < buttonProficiency.options.Count; i++) { 
            if (proficiency.ToLower() == buttonProficiency.options[i].text.ToLower()) { 
                setButtonProficiency(i); return; 
            } 
        }
    }
    public int getButtonProficiency() { return buttonProficiency.value; }
    public void setButtonProficiency(int index) { if(!pc.currentButton) { Debug.LogWarning("No button selected! Cannot set proficiency."); return; }
        pc.currentButton.data.proficiency = buttonProficiency.options[index].text; 
        buttonProficiency.value = index;
    }
}

