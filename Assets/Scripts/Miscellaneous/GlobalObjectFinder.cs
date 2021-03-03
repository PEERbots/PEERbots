using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalObjectFinder : MonoBehaviour {
    //Global Object Finder
    public static GameObject FindGameObjectWithTag(string tag) {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        if(objects.Length == 1) { 
        	return objects[0]; 
        } else if(objects.Length == 0) { 
        	Debug.LogWarning("Exactly one object must be tagged \""+tag+"\"! No objects found."); return null; 
        } else if(objects.Length > 1) { 
        	Debug.LogError("Exactly one object must be tagged \""+tag+"\"! Multiple objects found."); return objects[0]; 
        } else {
        	Debug.LogError("Exactly one object must be tagged \""+tag+"\"! WTF NEGATIVE OBJECT COUNT."); return null;
        }
    }
}
