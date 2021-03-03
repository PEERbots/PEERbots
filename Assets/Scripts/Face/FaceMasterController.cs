using UnityEngine; 
using System.Collections.Generic;
using System.Collections;
using System;

public class FaceMasterController : MonoBehaviour {

    [Header("Advanced Face List")]
    public List<AdvancedFace> faces;

    [Header("Networking Variables")]
    public TankController tankController;
    public MP3MusicPlayer mp3MusicPlayer;
    public YoutubePlayer youtubePlayer;
    public EasyAgoraController agora;
    public UDPReceiver udpReceiver;
    
    [Header("TTS Connections")]
    public TextToSpeechSpeaker speaker;

    [Header("Color Settings")]
    public PEERbotMappings mappings;

    void Start() { //Prepare listeners
        if(udpReceiver) udpReceiver.onDataReceived += processJSONevent;
        if(agora) agora.onDataReceived += processJSONevent;   
    }

    //--------Quick blink command--------//
    public void blink() {
        foreach (AdvancedFace face in faces) { face.blink(); }
    }
    //--------JSON Parsing--------//
    public void processJSONevent(string jsonStr) {
        //UnityMainThreadDispatcher.Instance().Enqueue(_processJSONevent(jsonStr)); 
        processJSON(jsonStr);
    }
    IEnumerator _processJSONevent(string jsonStr) {
        processJSON(jsonStr);
        yield return null;
    }
    public bool processJSON(string jsonStr) {
        //Check if message is actually quick blink command:
        if(jsonStr == "blink") { blink(); return true; }
        //Otherwise, attempt to fully parse and platy JSON
        Debug.Log("PEERbotFaceServer Received: " + jsonStr);
        try { setFaceData(JsonUtility.FromJson<PEERbotButtonDataFull>(jsonStr)); return true; } 
        catch (Exception e) { Debug.LogError("JSON Parse failed. Here is the failed string: " + jsonStr); }
        return false;
    }
    //--------Applying face Data--------//
    public void setFaceData(PEERbotButtonDataFull data) { if (data == null) { Debug.LogWarning("PEERbotButtonDataFull to play is null! Ignorning."); }
        try {
            //Try to play Youtube or MP3 if exist
            bool youtube = tryLoadYoutube(data.speech);
            bool music = tryLoadMP3(data.speech);
            //Say TTS!     
            speaker.stopTTS();
            if(!music && !youtube) { speaker.sayTTS(data.speech, data.volume, data.rate, data.pitch); }        
            //Apply to all faces
            foreach(AdvancedFace face in faces) {
                face.setExpression(mappings.getEmotionIndexFromString(data.emotion));
                face.setFaceColor(new Color(data.r / 255f, data.g / 255f, data.b / 255f));
            }
        } catch (Exception e) { Debug.LogException(e, this); }
    }
    //Experimental YouTube player
    public bool tryLoadYoutube(string url) {
        if(youtubePlayer != null) {
            youtubePlayer.Stop();
            youtubePlayer.HideLoading(); 
            youtubePlayer.OnVideoPlayerFinished();
            //If detect Youtube video, play Youtube video!
            if(youtubePlayer.TryNormalizeYoutubeUrlLocal(url, out url)) {
                Debug.Log(url);
                youtubePlayer.ShowLoading();
                youtubePlayer.Play(url);                    
                return true;                    
            }
        } return false;
    }
    //Experimental MP3 player
    public bool tryLoadMP3(string filename) {
        if(mp3MusicPlayer != null) { 
            mp3MusicPlayer.StopMP3(); 
            if(filename.Length > 4 && filename.Substring(filename.Length - 4).ToLower() == ".mp3") {
                mp3MusicPlayer.PlayMP3FromFile(filename);
                return true;
            } 
        } return false;
    }
}

[System.Serializable] 
public class TankSpeed {
    public int x = 0;
    public int y = 0;
}

[System.Serializable] 
public class TextureExpression {
    public string name;
    public Texture mouth;
    public Texture eyeLeft;
    public Texture eyeRight;
    public Vector3 eyeScale = new Vector3(1,1,1);
    public Vector3 mouthScale = new Vector3(1,1,1);
}