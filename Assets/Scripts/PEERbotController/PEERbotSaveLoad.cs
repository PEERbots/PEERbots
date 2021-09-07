using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleFileBrowser;
using SFB;
// Uses the following file pickers:
// 1. Runtime File Browser: https://assetstore.unity.com/packages/tools/integration/native-file-picker-for-android-ios-173238
// 2. Native File Picker: https://assetstore.unity.com/packages/tools/gui/runtime-file-browser-113006
// 3. Standalone File Picker: https://github.com/gkngkc/UnityStandaloneFileBrowser
public class PEERbotSaveLoad : MonoBehaviour {

    [Header("Connections")]
    public PEERbotController pc;
    public PEERbotButtonEditorUI editorUI;

    public enum FileBrowserMode { Both, Desktop, Mobile, Native } //Desktop for Mac/Win/Linux, Mobile for Android, Native for iOS
    public FileBrowserMode fileBrowserMode;

    public GameObject androidSaveLoadBackground;
    private static string SLASH;
    private static string defaultPalettePath;
    public string defaultJSONPath;

    public bool loadStreamingAssetsPalettes = true;
    public bool loadAllOnInit = false;
    
    public string EditorApkPath = "BetterStreamingAssetsTest.apk";

    ///***************************************************///
    ///**************INITIALIZATION FUNCTIONS*************///
    ///***************************************************///
    void Start() {
        SLASH = (Application.platform == RuntimePlatform.Android ||
                 Application.platform == RuntimePlatform.OSXPlayer ||
                 Application.platform == RuntimePlatform.OSXEditor ||
                 Application.platform == RuntimePlatform.IPhonePlayer ||
                 Application.platform == RuntimePlatform.WindowsEditor ||
                 Application.platform == RuntimePlatform.WindowsPlayer)?"/":"\\";

        //defaultPalettePath = Application.streamingAssetsPath + "/Palettes";
        defaultPalettePath = Application.persistentDataPath; //SLASH+ "/Palettes";

        //Force set log path        
        if(Application.platform == RuntimePlatform.Android || 
           Application.platform == RuntimePlatform.IPhonePlayer) {
            defaultJSONPath = Application.persistentDataPath;
        } else {
            defaultJSONPath = Application.streamingAssetsPath;
        }

        //Load all pre-baked CSV in streaming assets folder
        if(loadStreamingAssetsPalettes) {
            BetterStreamingAssets.Initialize();
            string[] paths;
            //f*cking Android gotta be special        
            if(Application.platform == RuntimePlatform.Android) { paths = BetterStreamingAssets.GetFiles("Baked", "*.csv", SearchOption.AllDirectories); }
            else { paths = Directory.GetFiles(Application.streamingAssetsPath + SLASH + "Baked", "*.csv"); }
            foreach(string path in paths) { 
                string _path = path.Replace("\\",SLASH).Replace("/",SLASH);
                //Ignore specially tagged palettes
                string filename = getPaletteNameFromFilePath(_path);
                if(filename.Length >= 5 && filename.Substring(0, 5) == "[LOG]") { Debug.Log("Found Log File. Ignoring"); continue; }
                //Load everything else
                LoadCSVPaletteFromStreamingAssets(_path); 
            }
        }

        //Load all use-generated CSV
        //if(loadAllOnInit) { LoadAllCSVPalettes(); }
        if(loadAllOnInit) { LoadAllJSONPalettes(); }
        
        //Init SImple File Browser (Mobile Android File Browser)
        InitSimpleFileBrowser();
    }

    //Initializing Save/Load Browser
    void InitSimpleFileBrowser() {  //Android only.
        SimpleFileBrowser.FileBrowser.SetFilters( true, ".csv" );
        SimpleFileBrowser.FileBrowser.SetDefaultFilter( ".csv" );
        SimpleFileBrowser.FileBrowser.SingleClickMode = true;
        SimpleFileBrowser.FileBrowser.RequestPermission();
    }

    ///***************************************************///
    ///***************CSV LOADING FUNCTIONS***************///
    ///***************************************************///
    public void LoadAllCSVPalettes() {
        string[] filePaths = System.IO.Directory.GetFiles(getPalettePathFolder(), "*.csv");
        foreach(string filePath in filePaths) { Debug.Log(filePath);
            //Ignore specially tagged palettes
            string filename = getPaletteNameFromFilePath(filePath);
            if(filename.Length >= 5 && filename.Substring(0, 5) == "[LOG]") { Debug.Log("Found Log File. Ignoring"); continue; }
            //Load everything else
            LoadCSVPalette(pc.newPalette(), filePath); 
        }
    }
    public void LoadAllJSONPalettes() {
        string[] filePaths = System.IO.Directory.GetFiles(defaultJSONPath, "*.json");
        foreach(string filePath in filePaths) { Debug.Log(filePath);
            //Delete the palette if it already exists
            string name = getPaletteNameFromFilePath(filePath);
            PEERbotPalette deleteMe = null;
            foreach(PEERbotPalette palette in pc.palettes) { if(palette.name == name) { deleteMe = palette; } }
            pc.deletePalette(deleteMe);
            //Load the palette
            LoadJSONPalette(pc.newPalette(), filePath); 
        }
    }
    //Needed for damn Android StreamingAssets
    public void LoadCSVPaletteFromStreamingAssets(string path) { 
        if(Application.platform == RuntimePlatform.Android) { //f*cking Android gotta be special        
            try{
                string encodedString = BetterStreamingAssets.ReadAllText(path);
                string filename = getPaletteNameFromFilePath(path);            
                LoadCSVPaletteFromEncodedString(pc.newPalette(true), encodedString, filename); 
            } catch(Exception e) {
                Debug.LogWarning("Failed to load CSV: " + path); return;
            }  
        } else {
            LoadCSVPalette(pc.newPalette(true), path);
        }
    }
    public void LoadCSVPalette(string path) { LoadCSVPalette(pc.newPalette(), path); }
    public PEERbotPalette LoadCSVPalette(PEERbotPalette palette, string path) {
        try{
            //Try to load the file (raw)
            StreamReader reader = new StreamReader(path); 
            string encodedString = reader.ReadToEnd();//.ToLower();
            reader.Close();
            //Try to parse the read encodedString
            string filename = getPaletteNameFromFilePath(path);
            PEERbotPalette newPalette = LoadCSVPaletteFromEncodedString(palette, encodedString, filename);
            //If successfully loaded, set the new path
            if(newPalette != null) { setHomePath(getPalettePathFolder(path)); }
            //Return the result
            return newPalette;
        } catch(Exception e) {
            Debug.LogWarning("Failed to load CSV: " + path); return palette;
        }        
    }
    public PEERbotPalette LoadCSVPaletteFromEncodedString(PEERbotPalette palette, string encodedString, string filename) {
        //Try to load the CSV first
        string[][] table;
        try{
            table = CsvParser2.Parse(encodedString);   
        } catch(Exception e) {
            Debug.LogWarning("Failed to load malformed CSV: " + filename); return palette;
        }
        //check and make sure there is content if parsed correctly
        if(table.Length == 0) { Debug.LogWarning("No rows, empty file: " + filename); return palette; }
        else if(table[0].Length == 0) { Debug.LogWarning("No cols, empty file: " + filename); return palette; }
        //Get rows/cols of table
        int rows = table.Length;
        int cols = table[0].Length;
        //Get a list of all the fields in the CSV
        List<string> fields = new List<string>();
        for(int j = 0; j < cols; j++) { 
            //try to automap to known palette format(s) for backwards compatibility
            //Debug.Log("Before: " + table[0][j]);
            if(table[0][j] == "TITLE(text)") { table[0][j] = "title"; }
            if(table[0][j] == "COLOR(0-7)") { table[0][j] = "color"; }
            if(table[0][j] == "EMOTION(0-7)") { table[0][j] = "emotion"; }
            if(table[0][j] == "SPEECH(text)") { table[0][j] = "speech"; }
            if(table[0][j] == "RATE(0.0-3.0)") { table[0][j] = "rate"; }
            if(table[0][j] == "PITCH(0.0-2.0)") { table[0][j] = "pitch"; }
            if(table[0][j] == "GOAL(text)") { table[0][j] = "goal"; }
            if(table[0][j] == "SUBGOAL(text)") { table[0][j] = "subgoal"; }
            if(table[0][j] == "PROFICIENCY(text)") { table[0][j] = "proficiency"; }
            //Debug.Log("After: " + table[0][j]);
            //Add to the list of fields
            fields.Add("\"" + table[0][j]+ "\"");         
        }
        //Load every button from every row in CSV
        for(int i = 1; i < rows; i++) { string json = "{ ";
            for(int j = 0; j < cols; j++) {        
                json += fields[j] + ":" + "\"" + table[i][j]+ "\"";
                json += (j != cols - 1) ? ", " : "}";
            }
            //Debug.Log(json);
            PEERbotButton newButton = pc.newButton();
            JsonUtility.FromJsonOverwrite(json, newButton.data);
            pc.selectButton(newButton);
        }
        //Fill the title and path
        palette.title = filename;
        //Select the newly created palette
        pc.selectPalette(palette);
        //Convert to JSON
        //SaveCurrentJSONPalette();
        //Return the result
        return palette;
    }

    public void LoadJSONPalette(string path) { LoadJSONPalette(pc.newPalette(), path); }
    public PEERbotPalette LoadJSONPalette(PEERbotPalette palette, string path) {
        //Try to load the CSV first
        string encodedString = "";
        try{
            StreamReader reader = new StreamReader(path); 
            encodedString = reader.ReadToEnd();//.ToLower();
            reader.Close();
        } catch(Exception e) {
            Debug.LogWarning("Failed to load JSON: " + path); return palette;
        }
        
        //overwrite the palette if it exists
        PEERbotPaletteData paletteData = JsonUtility.FromJson<PEERbotPaletteData>(encodedString);
        palette.title = paletteData.title;
        
        //Create new buttons from newPalette template
        foreach(PEERbotButtonData buttonData in paletteData.buttons) {
            PEERbotButton buttonClone = pc.newButton();
            buttonClone.data = JsonUtility.FromJson<PEERbotButtonDataFull>(JsonUtility.ToJson(buttonData));
            pc.selectButton(buttonClone);
        }
    
        //Select the newly creatde palette
        pc.selectPalette(palette);
        //If successfully loaded, set the new path
        setHomePath(getPalettePathFolder(path));
        //Return the result
        return palette;
    }
    
    //Save Browser on click
    public void loadPaletteOnClick() {
        //If mode selected, force choose a file browser type
        if(fileBrowserMode == FileBrowserMode.Desktop) { DesktopFileBrowserLoadPalette(); return; }
        if(fileBrowserMode == FileBrowserMode.Mobile) { MobileFileBrowserLoadPalette(); return; }
        if(fileBrowserMode == FileBrowserMode.Native) { NativeFileBrowserLoadPalette(); return; }
        //Desktop (Mac/Win/Linux)
        if(Application.platform == RuntimePlatform.OSXEditor ||
           Application.platform == RuntimePlatform.OSXPlayer ||
           Application.platform == RuntimePlatform.LinuxEditor ||
           Application.platform == RuntimePlatform.LinuxPlayer ||
           Application.platform == RuntimePlatform.WindowsEditor ||
           Application.platform == RuntimePlatform.WindowsPlayer) { DesktopFileBrowserLoadPalette(); return; }
        //Mobile (Android)
        if(Application.platform == RuntimePlatform.Android) { MobileFileBrowserLoadPalette(); return; }
        //Native (iOS)
        if(Application.platform == RuntimePlatform.IPhonePlayer) { NativeFileBrowserLoadPalette(); return; }
    }
    //Simple File Browser Load Settings
    public void MobileFileBrowserLoadPalette() {
        androidSaveLoadBackground.SetActive(true);
        StartCoroutine( MobileFileBrowserLoadCoroutinePalette(pc.newPalette()) );
    }
    IEnumerator MobileFileBrowserLoadCoroutinePalette(PEERbotPalette palette) {
        SimpleFileBrowser.FileBrowser.SetDefaultFilter( ".csv" );
        yield return SimpleFileBrowser.FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, getPalettePathFolder(), null, "Load CSV Palette File", "Load" );
        androidSaveLoadBackground.SetActive(false);
        if(FileBrowser.Success) { //Try to load if successful
            //Because F%^$ing Android refuses to keep file IO simple, with their F!@# stupid SAF system
            string filepath = FileBrowserHelpers.GetDirectoryName(FileBrowser.Result[0]);
            string filename = FileBrowserHelpers.GetFilename(FileBrowser.Result[0]);
            string realpath = Path.Combine(filepath, filename);
            Debug.Log("Realpath: " + realpath);        
            LoadCSVPalette(palette, realpath); 
        }
        else { pc.deletePalette(palette); }
    }
    //Standalone File Browser Load Settings
    public void DesktopFileBrowserLoadPalette() {
        var extensions = new [] { new ExtensionFilter("Files", "csv") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", getPalettePathFolder(), extensions, true);
        foreach(string path in paths) {
            LoadCSVPalette(pc.newPalette(), path);
        }
    }    
    //Native File Picker Load (for Android and iOS)
    public void NativeFileBrowserLoadPalette() {
        string[] allowedFileTypes = {NativeFilePicker.ConvertExtensionToFileType("csv")};
        NativeFilePicker.PickFile(LoadCSVPalette, allowedFileTypes);
        if(pc.currentPalette.buttons.Count == 0) { pc.deletePalette(pc.currentPalette); }
    }

    ///**************************************************///
    ///***************CSV SAVING FUNCTIONS***************///
    ///**************************************************///
    /* public void SaveAllCSVPalettes() {
        if (pc.getPaletteCount() <= 0) { Debug.Log("No palettes to save! Nothing saved."); }
        foreach(PEERbotPalette palette in pc.palettes) {
            Debug.Log(getPalettePathFolder() + SLASH + palette.title  + ".csv");
            SaveCSVPalette(palette, getPalettePathFolder() + SLASH + palette.title  + ".csv");
        }
    } */

    public void DeleteCurrentJSONPaletteFile() {
        DeletePaletteFileFromPath(Path.Combine(defaultJSONPath, pc.currentPalette.title + ".json"));
    }

    public void DeleteJSONPaletteFileWithName(string name) {
        DeletePaletteFileFromPath(Path.Combine(defaultJSONPath, name + ".json"));
    }

    public void DeletePaletteFileFromPath(string path) {
        try { 
            if(File.Exists(path)) { File.Delete(path); Debug.Log("File deleted at path: " + path); }
        } catch(Exception e) {
            Debug.Log("Failed to delete file at path. " + e);
        }
    }

    public void SaveCSVPalette(PEERbotPalette palette, string path) {
        //Check and make sure path and path are not null.
        if(palette == null) { Debug.LogWarning("Palette is null! Cannot save palette."); return; }
        if(string.IsNullOrEmpty(path)) { Debug.LogWarning("Path is null! Cannot save palette."); return; }        
        //Change Palette Name to Path Name (if different)
        //editorUI.setPaletteTitle(getPaletteNameFromFilePath(path));    
        //Convert button list to button data list, 
        List<PEERbotButtonDataFull> buttonDataListFull = palette.buttons.ConvertAll(x => x.data);
        //And then crimp from full data to simplified data        
        List<PEERbotButtonData> buttonDataList = buttonDataListFull.ConvertAll(x => JsonUtility.FromJson<PEERbotButtonData>(JsonUtility.ToJson(x)));
        try {
            //Save using sinban CSV auto json parser
            Sinbad.CsvUtil.SaveObjects(buttonDataList, path);
            //If successfully saved, set the new path
            setHomePath(getPalettePathFolder(path));
        } catch(Exception e) {
            Debug.LogWarning(e);
        }
    }
    
    public void SaveJSONPalette(PEERbotPalette palette, string path) {
        //Check and make sure path and path are not null.
        if(palette == null) { Debug.LogWarning("Palette is null! Cannot save palette."); return; }
        if(string.IsNullOrEmpty(path)) { Debug.LogWarning("Path is null! Cannot save palette."); return; }

        //Change Palette Name to Path Name (if different)
        editorUI.setPaletteTitle(Path.GetFileNameWithoutExtension(path));    
        
        //Convert Waseda Palette to Waseda Palette Data (for JSON serialization)
        PEERbotPaletteData paletteData = new PEERbotPaletteData();
        paletteData.title = palette.title;
        paletteData.buttons = palette.buttons.ConvertAll(x => JsonUtility.FromJson<PEERbotButtonData>(JsonUtility.ToJson(x.data)));

        // Create a file to write to.
        string json = JsonUtility.ToJson(paletteData);
        File.WriteAllText(path, json);
        Debug.Log(json);
    }

    public void SaveCurrentJSONPalette() {
        if(pc.currentPalette == null) { Debug.LogWarning("CurrentPalette is null! Cannot save palette."); return; }
        SaveJSONPalette(pc.currentPalette, defaultJSONPath + SLASH + pc.currentPalette.title  + ".json");
    }

    public void SaveCurrentCSVPalette() {
        if(pc.currentPalette == null) { Debug.LogWarning("CurrentPalette is null! Cannot save palette."); return; }
        SaveCSVPalette(pc.currentPalette, getPalettePathFolder() + SLASH + pc.currentPalette.title  + ".csv");
    }

    //Save Browser on click
    public void savePaletteOnClick() {
        //If mode selected, force choose a file browser type
        if(fileBrowserMode == FileBrowserMode.Desktop) { DesktopFileBrowserSavePalette(); return; }
        if(fileBrowserMode == FileBrowserMode.Mobile) { MobileFileBrowserSavePalette(); return; }
        if(fileBrowserMode == FileBrowserMode.Native) { NativeFileBrowserSavePalette(); return; }
        //Desktop (Mac/Win/Linux)
        if(Application.platform == RuntimePlatform.OSXEditor ||
           Application.platform == RuntimePlatform.OSXPlayer ||
           Application.platform == RuntimePlatform.LinuxEditor ||
           Application.platform == RuntimePlatform.LinuxPlayer ||
           Application.platform == RuntimePlatform.WindowsEditor ||
           Application.platform == RuntimePlatform.WindowsPlayer) { DesktopFileBrowserSavePalette(); return; }
        //Mobile (Android)
        if(Application.platform == RuntimePlatform.Android) { MobileFileBrowserSavePalette(); return; }
        //Native (iOS)
        if(Application.platform == RuntimePlatform.IPhonePlayer) { NativeFileBrowserSavePalette(); return; }
    }

    //Simple File Browser Save Settings
    public void MobileFileBrowserSavePalette() {
        if(pc.currentPalette == null) { Debug.LogWarning("No Palette selected! Cannot save."); return; }        
        androidSaveLoadBackground.SetActive(true);
        StartCoroutine( MobileFileBrowserSaveCoroutinePalette(pc.currentPalette) );
    }
    IEnumerator MobileFileBrowserSaveCoroutinePalette(PEERbotPalette palette) {
        SimpleFileBrowser.FileBrowser.SetDefaultFilter( ".csv" );
        yield return SimpleFileBrowser.FileBrowser.WaitForSaveDialog(FileBrowser.PickMode.Files, false, getPalettePathFolder(), palette.title, "Save CSV Palette File", "Save" );
        androidSaveLoadBackground.SetActive(false);
        if(SimpleFileBrowser.FileBrowser.Success) { SaveCSVPalette(palette, SimpleFileBrowser.FileBrowser.Result[0]); }
    }
    //Standalone File Browser Save Settings
    public void DesktopFileBrowserSavePalette() { if(pc.currentPalette == null) { Debug.LogWarning("No Palette selected! Cannot save."); return; }        
        // Multiple save extension filters with more than one extension support.
        var extensions = new [] { new ExtensionFilter("Files", "csv") };
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", getPalettePathFolder(), pc.currentPalette.title, extensions);
        SaveCSVPalette(pc.currentPalette, path); 
    }    
    //Native File Picker Save (for Android and iOS)
    public void NativeFileBrowserSavePalette() { if(pc.currentPalette == null) { Debug.LogWarning("No Palette selected! Cannot save."); return; }        
        //Create temporary file path to create a CSV:
        string filePath = Path.Combine( Application.temporaryCachePath, pc.currentPalette.title + ".csv" );
        SaveCSVPalette(pc.currentPalette, filePath);

        // Export the selected file to the target directory
        NativeFilePicker.Permission permission = NativeFilePicker.ExportFile( filePath, ( success ) => Debug.Log( "File exported: " + success ) );
        Debug.Log( "Permission result: " + permission );
    }

    ///**********************************************///
    ///************SET HOME FOLDER FUNCTIONS*********///
    ///**********************************************///

    //Set Home Path on Click
    public void setHomePathOnClick() {
        //If mode selected, force choose a file browser type
        if(fileBrowserMode == FileBrowserMode.Desktop) { DesktopFileBrowserSetHome(); return; }
        if(fileBrowserMode == FileBrowserMode.Mobile) { MobileFileBrowserSetHome(); return; }
        if(fileBrowserMode == FileBrowserMode.Native) { Debug.Log("No Home Path on iOS. Yay Apple!"); return; }
        //Desktop (Mac/Win/Linux)
        if(Application.platform == RuntimePlatform.OSXEditor ||
           Application.platform == RuntimePlatform.OSXPlayer ||
           Application.platform == RuntimePlatform.LinuxEditor ||
           Application.platform == RuntimePlatform.LinuxPlayer ||
           Application.platform == RuntimePlatform.WindowsEditor ||
           Application.platform == RuntimePlatform.WindowsPlayer) { DesktopFileBrowserSetHome(); return; }
        //Mobile (Android)
        if(Application.platform == RuntimePlatform.Android) { MobileFileBrowserSetHome(); return; }
        //Native (iOS)
        if(Application.platform == RuntimePlatform.IPhonePlayer) {  Debug.Log("No Home Path on iOS. Yay Apple!"); return; }
    }
    //Simple File Browser Home Path Settings
    public void MobileFileBrowserSetHome() {
        androidSaveLoadBackground.SetActive(true);
        StartCoroutine( _MobileFileBrowserSetHome() );
    }
    IEnumerator _MobileFileBrowserSetHome() {
        yield return SimpleFileBrowser.FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Folders, true, getPalettePathFolder(), null, "Set Home Path Folder", "Load" );
        androidSaveLoadBackground.SetActive(false);
        if(SimpleFileBrowser.FileBrowser.Success) { 
            //Because F%^$ing Android refuses to keep file IO simple, with their F!@# stupid SAF system
            string realpath = FileBrowserHelpers.GetDirectoryName(SimpleFileBrowser.FileBrowser.Result[0] + SLASH + "idontexist.csv");
            setHomePath(realpath); 
        }
    }
    //Native File Picker Home Path
    public void DesktopFileBrowserSetHome() { 
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Set Home Path Folder", getPalettePathFolder(), false);
        foreach(string path in paths) { setHomePath(path); }
    } 

    ///**********************************************///
    ///***************EMAILING FUNCTIONS*************///
    ///**********************************************///
    public void NativeShare() { if(pc.currentPalette == null) { Debug.LogWarning("No palette selected! Nothing saved."); return; }
        string filename = pc.currentPalette.title  + ".csv";
        string path = getPalettePathFolder() + SLASH + filename;
        
        new NativeShare().AddFile(path)
                        .SetSubject("Shared PEERbots File: \"" + filename + "\"")
                        .SetText("Sent \"" + filename + "\" on " + System.DateTime.Now.ToString("yyyy-MM-dd hh:mm:sstt") + ". Enjoy!")
                        .SetCallback( ( result, shareTarget ) => Debug.Log( "Share CSV palette: " + result + ", selected app: " + shareTarget ) )
                        .Share();    
    }

    ///**********************************************///
    ///*************PATH HELPER FUNCTIONS************///
    ///**********************************************///
    public void setHomePath(string path) { 
        //if(!Uri.IsWellFormedUriString(path, UriKind.RelativeOrAbsolute)) { Debug.LogWarning("Log Path not valid: " + path); return; }
        PlayerPrefs.SetString("PalettePath", path);
        Debug.Log("Set Home Path to: " + path);
    }
    public List<string> getPalettePaths() {
        List<string> paths = new List<string>();
        foreach(PEERbotPalette palette in pc.palettes) { paths.Add(getPalettePathFolder() + SLASH + palette.title  + ".csv"); }
        return paths;
    }
    public static string getPalettePathFolder(string path = null) {
        //If path is null, just return current palette path
        if(path == null) { return PlayerPrefs.GetString("PalettePath", defaultPalettePath); }
        //MAKE SURE THE SLASH IS RIGHT
        path = path.Replace("\\","/");
        //If no slashes, just return default path
        if(path.LastIndexOf(SLASH) < 0) { return PlayerPrefs.GetString("PalettePath", defaultPalettePath); }
        //Return the folder
        return path.Substring(0, path.LastIndexOf(SLASH));
    }
    public static string getPaletteNameFromFilePath(string path) { path = path.Replace("\\","/");
        return Path.GetFileNameWithoutExtension(path);
        //path = path.Substring(path.LastIndexOf(SLASH) + 1);
        //return path.Substring(0, path.Length-4);
    }

    //File name parser helper
    public static string SanitizeFilename(string text) { if(text == null) { return null; }
        // Create  a string array and add the special characters you want to remove
        string[] chars = new string[] { "#", "%", "&", "{", "}", "\\", "<", ">", "*", "?", "/", "$", "!", "'", "\"", ":", "@", "|", ";", "." };
        //Iterate the number of times based on the String array length.
        for (int i = 0; i < chars.Length; i++) { if (text.Contains(chars[i])) { text = text.Replace(chars[i], ""); } }
        return text;
    }
}
