using UnityEngine;
using UnityEngine.UI;

using System.IO;
using System.Collections;
using System.Collections.Generic;

public class PEERbotMappings : MonoBehaviour {
  [Header("Emotion Mappings")]
  public List<EmotionMap> emotions;
  
  [Header("Color Mappings")]
  public List<ColorMap> colors;

  [Header("Goal Mappings")]
  public List<GoalMap> goals;

  public int getEmotionIndexFromString(string text) {
    //Check if text is already "int"
    int tryInt = 0; if(int.TryParse(text, out tryInt)) { return tryInt; }
    //If text is "emotion", try to parse and map it to int
    for(int i = 0; i < emotions.Count; i++) {
      if(text.ToLower() == emotions[i].defaultEmotion.ToLower()) { return i; }
    }
    return 0;
  }

  public int getColorIndexFromString(string text) {
    //Check if text is already "int"
    int tryInt = 0; if(int.TryParse(text, out tryInt)) { return tryInt; }
    //If text is "color", try to parse and map it to int  
    for(int i = 0; i < colors.Count; i++) {
      if(text.ToLower() == colors[i].name.ToLower()) { return i; }
    }
    return 0;
  }

}

[System.Serializable]
public class ColorMap {
  public string name;
  public Color32 color;
  public ColorMap(string n, Color32 c) { name = n; color = c; }
}  

[System.Serializable]
public class EmotionMap {
  //String mappings
  public string defaultEmotion;
  public string mistyEmotion;
  //Int Mappings
  public int expression = 0;
  public int leftEyeShape = 0; //set to -1 to ignore
  public int rightEyeShape = 0;
}  

[System.Serializable]
public class GoalMap {
  public string goal;
  public List<string> subgoals;
  [HideInInspector]
  public Dropdown subgoalDropdown;
  [HideInInspector] //Proficiency measurements  
  public int none, exposure, understanding, practicing, demonstrating = 0; 
}  
