
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

using SFB;

public class PEERbotLogger : MonoBehaviour {

    [Header("Connections")]
    public PEERbotController pc;

    [Header("File IO")]
    private static string SLASH;
    public static string logPath;
    
    [Header("Logging")]    
    public List<PEERbotButtonDataFull> log;
    public List<PEERbotPaletteLogData> paletteLog;
    public GameObject activeLabel;

    public string sessionID = "";
    private string timezone = "";
    private bool isLogging = false;
    public bool autoStartLogging = true;
    public static string lastPalette = "Initial Palette";

    void Awake() {
        //Get local timezone
        foreach(Match match in Regex.Matches(System.TimeZone.CurrentTimeZone.StandardName, "[A-Z]")) { timezone += match.Value; }
    
        //Set SLASH
        SLASH = (Application.platform == RuntimePlatform.Android ||
                 Application.platform == RuntimePlatform.OSXPlayer ||
                 Application.platform == RuntimePlatform.OSXEditor ||
                 Application.platform == RuntimePlatform.IPhonePlayer ||
                 Application.platform == RuntimePlatform.WindowsEditor ||
                 Application.platform == RuntimePlatform.WindowsPlayer)?"/":"\\";

        //Force set log path        
        if(Application.platform == RuntimePlatform.Android || 
           Application.platform == RuntimePlatform.IPhonePlayer) {
            logPath = Application.persistentDataPath + SLASH + "Logs";
        } else {
            logPath = Application.streamingAssetsPath + SLASH + "Logs";
        }
        System.IO.Directory.CreateDirectory(logPath);

        //Begin Logging
        if(autoStartLogging) { startLogging(); }
    }

    public void SetSessionID(string text) { if(text == null) { text = ""; }
        PEERbotSaveLoad.SanitizeFilename(text);
        sessionID = text;
    }

    ///**************************************************///
    ///***************CSV EXPORT FUNCTIONS***************///
    ///**************************************************///
    public void NativeShare() { 
        string filename = "[LOG] MasterLog.csv";
        string path = logPath + SLASH + filename;
        
        new NativeShare().AddFile(path)
                        .SetSubject("Shared PEERbots File: \"" + filename + "\"")
                        .SetText("Sent \"" + filename + "\" on " + System.DateTime.Now.ToString("yyyy-MM-dd hh:mm:sstt") + ". Enjoy!")
                        .SetCallback( ( result, shareTarget ) => Debug.Log( "Share CSV palette: " + result + ", selected app: " + shareTarget ) )
                        .Share();    
    }

    ///***************************************************///
    ///***************CSV LOGGING FUNCTIONS***************///
    ///***************************************************///
    public void AddToLog() { AddToLog(pc.currentPalette, pc.currentButton.data); }
    public void AddToLog(PEERbotPalette palette, PEERbotButtonDataFull data) { 
        if(!isLogging) { return; }
        if (palette == null || data ==  null) 
        {
            Debug.LogWarning("Palette or button is null! Cannot add to Log!");
            return;
        }
        data.palette = palette.title;
        data.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        data.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        log.Add(data);
        //Add to master log file
        List<PEERbotButtonDataFull> single = new List<PEERbotButtonDataFull>(); single.Add(data);
        Sinbad.CsvUtil.SaveObjects(single, logPath + SLASH + "[LOG] MasterLog.csv", true);
    }

    
    public void AddToPaletteLog() { AddToPaletteLog(pc.currentPalette); }
    public void AddToPaletteLog(PEERbotPalette palette){
        if (palette == null)
        {
            Debug.LogWarning("Palette is null! Cannot add to PaletteLog!");
            return;
        }
        PEERbotPaletteLogData data = new PEERbotPaletteLogData();
        data.newValue = palette.title;
        data.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        data.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        data.previousValue = lastPalette;
        paletteLog.Add(data);
        lastPalette = palette.title;
        /*
        //Add to master log file
        List<PEERbotPaletteLogData> single = new List<PEERbotPaletteLogData>(); single.Add(data);
        Sinbad.CsvUtil.SaveObjects(single, logPath + SLASH + "[LOG] MasterLog.csv", true);
        */
    }
    
    public void SaveLog(string logName) {
        //Check and make sure path and path are not null.
        if(log == null) { Debug.LogWarning("Log is null! Cannot save log."); return; }
        if(log.Count == 0) { Debug.LogWarning("Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(log, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }    

    public void SavePaletteLog(string logName) {
        //Check and make sure path and path are not null.
        if(paletteLog == null) { Debug.LogWarning("Log is null! Cannot save log."); return; }
        if(paletteLog.Count == 0) { Debug.LogWarning("Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(paletteLog, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }    


    public void startLogging() { 
        if(isLogging) { stopLogging(); }
        isLogging = true;
        if(activeLabel != null) { activeLabel.SetActive(true); }
        log = new List<PEERbotButtonDataFull>();
    }
    public void stopLogging() {
        if(isLogging) {
            SaveLog("[LOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            SavePaletteLog("[PaletteLOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            isLogging = false;
            if(activeLabel != null) { activeLabel.SetActive(false); }
        } else {
            Debug.Log("Log not started, cannot save log.");
        }
    }
    public void OnApplicationQuit() { stopLogging(); }
    void OnDestroy() { stopLogging(); }

    ///**********************************************///
    ///*************PATH HELPER FUNCTIONS************///
    ///**********************************************///
    public void StandaloneFileBrowserSelectFolder() { 
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
        foreach(string path in paths) { setLogPath(path); }
    } 

    public void setLogPath(string path) { 
        PlayerPrefs.SetString("LogPath", path);
        logPath = path;
    }

}
