using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using SFB;

public class WAVFileHandler : MonoBehaviour {
    
    public event Action<AudioClip> onAudioClipParsed;

    public AudioSource audioSource;
    
    public UDPSender udpSender;
    public UDPReceiver udpReceiver;

    public InputField pathField;

    void Start() {
        if(udpReceiver) { udpReceiver.onDataReceived += processJSONevent; }
    }

    //**************************//
    //*** PLAYING AUDIO CLIP ***// (from file)
    //**************************//
    public void PlayAudioFromFilename(string filename) { 
        PlayAudioFromFile(Path.Combine(getPath(), filename));
    }
    public void PlayAudioFromFile(string path) { if(String.IsNullOrEmpty(path)) { Debug.Log("Path is null or empty! Can't play audio."); return; }
        if(!File.Exists(path)) { Debug.LogWarning("Cannot load WAV! File does not exist."); }
        byte[] bytes = File.ReadAllBytes(path);
        PlayAudioFromBytes(bytes);
    }
    public void PlayAudioFromBase64String(string base64String) { if(string.IsNullOrEmpty(base64String)) { return; }
        byte[] bytes = Convert.FromBase64String(base64String);
        PlayAudioFromBytes(bytes);
    }
    public void PlayAudioFromBytes(byte[] bytes) {
        if(bytes == null || bytes.Length == 0) { Debug.LogWarning("Cannot play audio from null or empty bytes!"); return; }
        AudioClip audioClip = OpenWavParser.ByteArrayToAudioClip(bytes);
        if(audioSource != null) { audioSource.clip = audioClip; audioSource.Play(); }
    }
    //Standalone File Browser Load Settings
    public void DesktopFileBrowserLoadWAV() {
        var extensions = new [] { new ExtensionFilter("Files", "wav") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", getPath(), extensions, false);
        if(paths.Length > 0) { PlayAudioFromFile(paths[0]); }
    }    

    //**************************//
    //*** GETTING AUDIO CLIP ***//
    //**************************//
    public string GetAudioStringFromFile(string path) { if(String.IsNullOrEmpty(path)) { Debug.Log("Path is null or empty! Can't load audio."); return ""; }        
        if(!File.Exists(path)) { Debug.LogWarning("Cannot load WAV! File does not exist."); }
        byte[] bytes = File.ReadAllBytes(path);
        string base64String = Convert.ToBase64String(bytes);
        return base64String;
    }

    //**************************//
    //*** SENDING AUDIO CLIP ***//
    //**************************//
    public void SendAudioFromClip(AudioClip audioClip) { if(audioClip == null) { return; } 
        byte[] bytes = OpenWavParser.AudioClipToByteArray(audioClip);
        SendAudioFromBytes(bytes); 
    }
    public void SendAudioFromFile(string path) {  //string path = audiopath + "tts" + ".wav"; //Speaker.AudioFileExtension;
        if(String.IsNullOrEmpty(path)) { Debug.Log("Path is null or empty! Can't send audio."); return; }
        if(!File.Exists(path)) { Debug.LogWarning("Cannot load WAV! File does not exist."); }
        byte[] bytes = File.ReadAllBytes(path);
        SendAudioFromBytes(bytes);
    }
    public void SendAudioFromBytes(byte[] bytes) { 
        string base64String = Convert.ToBase64String(bytes);
        SendAudioFromBase64String(base64String);
    }
    public void SendAudioFromBase64String(string base64String) {
        WAVMessage wavMessage = new WAVMessage();
        wavMessage.base64string = base64String;
        udpSender?.sendMessage(JsonUtility.ToJson(wavMessage));
    }
    
    //**************************//
    //*** PARSING AUDIO CLIP ***//
    //**************************//
    public AudioClip ParseAudioFromString(string base64string) {
        if(String.IsNullOrEmpty(base64string)) { Debug.Log("Base64String is null or empty! Can't parse audio."); return null; }
        byte[] bytes = Convert.FromBase64String(base64string);
        return ParseAudioFromBytes(bytes);
    }
    public AudioClip ParseAudioFromBytes(byte[] bytes) {
        AudioClip audioClip = OpenWavParser.ByteArrayToAudioClip(bytes);
        OnAudioClipParsed(audioClip);
        return audioClip;
    }
    protected virtual void OnAudioClipParsed(AudioClip message) {
        onAudioClipParsed?.Invoke(message);
    }

    //******************************//
    //*** RECEIVING JSON MESSAGE ***//
    //******************************//
    public void processJSONevent(string jsonStr) {
        processJSON(jsonStr);
        //UnityMainThreadDispatcher.Instance().Enqueue(_processJSONevent(jsonStr)); 
    }
    IEnumerator _processJSONevent(string jsonStr) {
        processJSON(jsonStr);
        yield return null;
    }
    public bool processJSON(string jsonStr) {
        Debug.Log("Received: " + jsonStr);
        
        WAVMessage wavMessage = null;
        //Debug.Log("Received request to parse message. Attempting to parse...");
        try { wavMessage = JsonUtility.FromJson<WAVMessage>(jsonStr); } 
        catch (Exception e) { 
            Debug.LogError("JSON Parse failed. Here is the failed string: " + jsonStr); 
            return false;
        }
    
        if (wavMessage != null) { 
            //ParseAudioFromString(wavMessage.base64string); 
            PlayAudioFromBase64String(wavMessage.base64string);
            return true; 
        }
        else { return false; }
    }

    //-----------------------------------------------------//
    //-----------------PATH HOME FUNCTIONS-----------------//
    //-----------------------------------------------------//
    public void DesktopFileBrowserSetHome() { 
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Set WAV File Path Folder", getPath(), false);
        foreach(string path in paths) { setPath(path); }
    } 
    public void setPath(string path) { 
        PlayerPrefs.SetString("WAVFilePath", path); 
        Debug.Log("Set WAV File Path to: " + path); 
        if(pathField) pathField.text = path;
    }
    public string getPath() { 
        return PlayerPrefs.GetString("WAVFilePath", Application.persistentDataPath); 
    }

}

[System.Serializable]
public class WAVMessage {
    public string base64string = "";
}

