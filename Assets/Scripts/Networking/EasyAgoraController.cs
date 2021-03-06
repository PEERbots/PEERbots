using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using agora_gaming_rtc;
using agora_utilities;

using Assets.SimpleEncryption;

//Full Agora IO docs here: https://docs.agora.io/en/Video/API%20Reference/unity/index.html

public enum HostOrClientMode { BOTH, HOST, CLIENT }

public class EasyAgoraController : MonoBehaviour {

    [Header("Agora Settings")]
    [SerializeField] private string AppID = "your_appid"; // PLEASE KEEP THIS App ID IN SAFE PLACE. Get your own App ID at https://dashboard.agora.io/
    private IRtcEngine mRtcEngine; // instance of agora engine
    public HostOrClientMode hostOrClientMode;

    public int streamID;
    public int UID = 0;
    public int playerNumber = 0;
    public int playerCount = 0;
    public int maxPlayerCount = 3;
    public event Action<string> onDataReceived;
    
    public float timeout = -1; //set this to -1 if not in use. Resets upon joining, activity.
    public float timeoutTimer = -10;
    
    public bool useAudio = true;
    public bool useVideo = true;


    [Header("Local Video / Audio")]
    public bool muteLocalAudio = false;
    public bool muteLocalVideo = false;
    public int localVolume;  //local sensitivity of local mic 
    
    public GameObject localVideoObject;
    public bool isLocalVideo3D = false;
    
    [Header("Remote Video / Audio")]
    public bool muteRemoteAudio = false;
    public bool muteRemoteVideo = false;
    public int remoteVolume; //received volume of remote mics

    public GameObject remoteVideoObject;
    public bool isRemoteVideo3D = false;

    [Header("Roomname")]
    public string roomname = "";
    private const string saltKey = "FmvKWp4N-XpP*Xm&";

    public string roomnamePref = "roomname";
    
    public InputField roomnameField;
    public InputField messageField;

    public GameObject leaveButton;
    public GameObject joinButton;
    
    public GameObject activeLabel;

    public GameObject warningLabel;
    public Text warningLabelText;

    private string streamMessage = "";

    //------------------------------------------------------//
    //-----------------INIT UNITY UI FIELDS-----------------//
    //------------------------------------------------------//

    void Start() {
        if(roomnameField != null) { 
            roomname = PlayerPrefs.GetString(roomnamePref, "");
            roomnameField.text = roomname;
        }
        if(activeLabel) { activeLabel.SetActive(false); }
    }
    void OnApplicationQuit() { leave(); }

    void Update() { 
        if(timeoutTimer > 0) { timeoutTimer -= Time.deltaTime; } 
        else if(timeoutTimer > -1) { timeoutTimer = -10;
            if(activeLabel) { activeLabel.SetActive(false); }
            showWarningLabel("Agora Connection Timed Out! Please Rejoin.");
            leave();
        }
    }

    public void showWarningLabel(string warning)  { Debug.LogWarning(warning);
        if(warningLabelText) { warningLabelText.text = warning; }
        warningLabel?.SetActive(true);
    }
    public void hideWarningLabel() { warningLabel?.SetActive(false); }

    //------------------------------------------------------//
    //---------------UNITY UI BUTTON COMMANDS---------------//
    //------------------------------------------------------//
    private string Encrypt(string text) { return AES.Encrypt(text, saltKey); }
    private string Decrypt(string text) { return AES.Decrypt(text, saltKey); }

    public void setRoomname(string text) { 
        roomname = text; PlayerPrefs.SetString(roomnamePref, roomname); 
        roomnameField.text = roomname;
    }
    public void generateRoomname() { setRoomname(GenerateRandomAlphaNumericString(6)); }

    public static string GenerateRandomAlphaNumericString(int length = 6) {
        const string src = "abcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder sb = new StringBuilder();
        System.Random RNG = new System.Random();
        for (int i = 0; i < length; i++) { char c = src[RNG.Next(0, src.Length)]; sb.Append(c); }
        return sb.ToString();
    }

    public void onJoinButtonClicked() { join(Encrypt(roomname)); }

    public void showLocalVideo() { clearLocalVideo();
        if(localVideoObject != null) {
            VideoSurface videoSurface = localVideoObject.AddComponent<VideoSurface>() as VideoSurface; 
            if (videoSurface != null) {
                videoSurface.SetForUser(0); //0 is for local user
                //videoSurface.SetEnable(true);
                if(isLocalVideo3D) {
                    videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.Renderer);
                } else {
                    videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
                }              
                //videoSurface.SetGameFps(30);
            }
        } else { Debug.LogWarning("Cannot show local video because localVideoObject is null!"); }
    }
    public void showRemoteVideo(uint uid) { clearRemoteVideo();
        if(remoteVideoObject != null) {
            VideoSurface videoSurface = remoteVideoObject.AddComponent<VideoSurface>() as VideoSurface; 
            if (videoSurface != null) {
                videoSurface.SetForUser(uid);
                //videoSurface.SetEnable(true);
                if(isRemoteVideo3D) {
                    videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.Renderer);
                } else {
                    videoSurface.SetVideoSurfaceType(AgoraVideoSurfaceType.RawImage);
                }
                //videoSurface.SetGameFps(30);
            }
        } else { Debug.LogWarning("Cannot show remote video because remoteVideoObject is null!"); }
    }
    public void clearLocalVideo() {
        if(localVideoObject != null) {
            if(localVideoObject.GetComponent<VideoSurface>() != null) { 
                Destroy(localVideoObject.GetComponent<VideoSurface>()); 
            }
            if(isLocalVideo3D) {
                localVideoObject.GetComponent<Renderer>().material.mainTexture = null;
            } else {
                localVideoObject.GetComponent<RawImage>().texture = null;
            }
        }
    }
    public void clearRemoteVideo() {
        if(remoteVideoObject != null) {
            if(remoteVideoObject.GetComponent<VideoSurface>() != null) { 
                Destroy(remoteVideoObject.GetComponent<VideoSurface>()); 
            }
            if(isRemoteVideo3D) {
                remoteVideoObject.GetComponent<Renderer>().material.mainTexture = null;
            } else {
                remoteVideoObject.GetComponent<RawImage>().texture = null;
            }
        }
    }

    //------------------------------------------------------//
    //-------------------AGORA JOIN/LEAVE-------------------//
    //------------------------------------------------------//
    //Load / Unload Engine. Call this to init / destroy Agora
    public void loadEngine(string appId) { Debug.Log("initializeEngine");
        if (mRtcEngine != null) { Debug.Log("Engine exists. Please unload it first!"); return; }
        // init engine
        mRtcEngine = IRtcEngine.GetEngine(appId);
        // enable log
        mRtcEngine.SetLogFilter(LOG_FILTER.DEBUG | LOG_FILTER.INFO | LOG_FILTER.WARNING | LOG_FILTER.ERROR | LOG_FILTER.CRITICAL);
    }
    public void unloadEngine() { Debug.Log("calling unloadEngine");
        // delete and Place this call in ApplicationQuit
        if (mRtcEngine != null) { IRtcEngine.Destroy(); mRtcEngine = null; }
    }
    //Called to join or leave a session. Engine must be loaded first!
    public void join(string channel) {         
        if(String.IsNullOrEmpty(channel)) { Debug.LogWarning("Empty channel name. Ignoring join request."); return; }
        
        loadEngine(AppID); // load engine

        if (mRtcEngine == null) { return; }
        Debug.Log("calling join (channel = " + channel + ")");
        
        // set callbacks (optional)
        mRtcEngine.OnJoinChannelSuccess = onJoinChannelSuccess;
        mRtcEngine.OnLeaveChannel = onLeaveChannel;
        mRtcEngine.OnUserJoined = onUserJoined;
        mRtcEngine.OnUserOffline = onUserOffline;
        mRtcEngine.OnStreamMessage = onStreamMessage;
        mRtcEngine.OnRtcStats = onRtcStats;

        //set timeout timer (optional)
        if(timeout > 0) { timeoutTimer = timeout; hideWarningLabel(); }

        //Audio settings
        if(useAudio) {
            mRtcEngine.EnableAudio(); // enable audio in general
            mRtcEngine.EnableLocalAudio(!muteLocalAudio); //controls sending local out HARD STOP
            
            mRtcEngine.MuteLocalAudioStream(muteLocalAudio); //still sending, but not audible
            mRtcEngine.MuteAllRemoteAudioStreams(muteRemoteAudio); //still sending, but not audible

            mRtcEngine.AdjustRecordingSignalVolume(localVolume);
            mRtcEngine.AdjustPlaybackSignalVolume(remoteVolume);
        } else { mRtcEngine.DisableAudio(); } // disable video in general   
        
        //Begin video feed
        if(useVideo) {
            mRtcEngine.EnableVideo(); // enable video in general
            mRtcEngine.EnableVideoObserver();
            mRtcEngine.EnableLocalVideo(!muteLocalVideo); //controls sending local out HARD STOP

            mRtcEngine.MuteLocalVideoStream(muteLocalVideo); //Send local video to remote?
            mRtcEngine.MuteAllRemoteVideoStreams(muteRemoteVideo); //still receiving, but not visible

            //if(muteRemoteVideo) { mRtcEngine.DisableVideoObserver(); } //controls receiving video HARD STOP
            //else                { mRtcEngine.EnableVideoObserver(); }
            
            //Change Video feed settings
            VideoEncoderConfiguration videoConfig = new VideoEncoderConfiguration();
            videoConfig.dimensions.width = 1280;
            videoConfig.dimensions.height = 720;
            videoConfig.frameRate = FRAME_RATE.FRAME_RATE_FPS_15;
            mRtcEngine.SetVideoEncoderConfiguration(videoConfig);

            //WEBCAMS: Show list of all webcam devices
            mRtcEngine.GetVideoDeviceManager().CreateAVideoDeviceManager();
            int deviceCount = mRtcEngine.GetVideoDeviceManager().GetVideoDeviceCount(); 
            Debug.Log("Video device count: " + deviceCount);
            string deviceName = ""; string deviceId = "";
            for(int i = 0; i < deviceCount; i++) { //Show list of webcams
                mRtcEngine.GetVideoDeviceManager().GetVideoDevice(i, ref deviceName, ref deviceId);
                Debug.Log("Device[i] deviceName = \"" + deviceName + "\" | deviceId = \"" + deviceId + "\"");
            }
            //---Show current video device---
            mRtcEngine.GetVideoDeviceManager().GetCurrentVideoDevice(ref deviceId);
            Debug.Log("CurrentVideoDevice deviceId = \"" + deviceId + "\"");
            //---Pick the first one?---
            mRtcEngine.GetVideoDeviceManager().GetVideoDevice(0, ref deviceName, ref deviceId);            
            mRtcEngine.GetVideoDeviceManager().SetVideoDevice(deviceId);
        } else { mRtcEngine.DisableVideo(); } // disable video in general   

        //Join Channel
        UID = mRtcEngine.JoinChannel(channel, null, 0); // join channel
        
        // Optional: if a data stream is required, here is a good place to create it. 
        streamID = mRtcEngine.CreateDataStream(true, true);
        Debug.Log("initializeEngine done, data stream id = " + streamID);

        //Show local video
        showLocalVideo();

        //Hide Join Button and Show Leave button if assigned
        joinButton.SetActive(false);
        leaveButton.SetActive(true);

        //Hide warning label if still there
        hideWarningLabel();
    }
    public void leave() { if (mRtcEngine == null) { return; }
        Debug.Log("calling leave");
        mRtcEngine.LeaveChannel(); // leave channel
        mRtcEngine.DisableVideoObserver(); // deregister video frame observers in native-c code
        
        unloadEngine(); // delete engine

        //Delete Local and remote Video Surfaces
        clearLocalVideo();
        clearRemoteVideo();

        playerCount = 0; 
        playerNumber = 0;

        //Hide Leave Button and Show Join button if assigned
        joinButton.SetActive(true);
        leaveButton.SetActive(false);
    }

    //------------------------------------------------------//
    //------------AGORA MISCELLANEOUS FUNCTIONS-------------//
    //------------------------------------------------------//
    public string getSdkVersion() {
        string ver = IRtcEngine.GetSdkVersion();
        if (ver == "2.9.1.45") { ver = "2.9.2"; } // A conversion for the current internal version#
        else if (ver == "2.9.1.46") { ver = "2.9.2.1"; }
        return ver;
    }
    public void sendMessageButton() { 
        if(messageField == null) { Debug.LogWarning("Message Field is null. Please assign!"); }
        sendMessage(messageField.text); 
    }
    public void sendMessage(string text) {
        if (mRtcEngine == null) { Debug.LogWarning("mRTC Engine not initialized. Cannot send!"); }
        else {
            Debug.Log("Attempting to send stream message: " + text);
            mRtcEngine.SendStreamMessage(streamID, text);    
            //set timeout timer (optional)
            if(timeout > 0) { timeoutTimer = timeout; if(warningLabel) { warningLabel.SetActive(false); } }
        }
    }
    public string getMessage() {
        if (streamMessage != null) {
            string message = streamMessage;
            streamMessage = null;
            return message;
        } else {
            return null;
        }
    }

    //------------------------------------------------------//
    //-------------------AGORA CALLBACKS--------------------//
    //------------------------------------------------------//
    // implement engine callbacks
    private void onStreamMessage(uint userId, int streamId, string data, int length) {
        Debug.Log("Stream message received from userId [" + userId + "]: " + data);
        streamMessage = data;
        OnDataReceived(data);
        //set timeout timer (optional)
        if(timeout > 0) { timeoutTimer = timeout; }
    }
    protected virtual void OnDataReceived(string message) { 
        onDataReceived?.Invoke(message);
    }
    //Channel stats handler (updated every two seconds)
    private void onRtcStats(RtcStats stats) {
        //Get the current player count
        playerCount = (int)stats.userCount;

        //Player number readjusts according to last player joined/quit
        if(playerNumber == 0) { playerNumber = 1000; }
        if(playerCount < playerNumber) { playerNumber = playerCount; }

        //Kick the last player that joined if max player count reached.
        if(playerNumber > maxPlayerCount) {
            showWarningLabel("MaxPlayerCount reached. Kicking last player that joined."); 
            leave();
        }
        
        //Check if client only (kick if trying to host)
        if(hostOrClientMode == HostOrClientMode.CLIENT && playerCount == 1) {
            showWarningLabel("No host found. Please try another room name."); 
            leave();    
        }

        //Check if host only (kick if not hosting)
        if(hostOrClientMode == HostOrClientMode.HOST && playerNumber > 1) {
            showWarningLabel("Room already exists. Please change room name."); 
            leave();    
        }
    }
    // when this user joins
    private void onJoinChannelSuccess(string channelName, uint uid, int elapsed) {
        Debug.Log("JoinChannelSuccessHandler: uid = " + uid);
    }
    // when this user leaves
    private void onLeaveChannel(RtcStats stats) {
        Debug.Log("LeaveChannel.");
    }
    // When a remote user joined, this delegate will be called. 
    private void onUserJoined(uint uid, int elapsed) { 
        Debug.Log("onUserJoined: uid = " + uid + " elapsed = " + elapsed);        
        if(activeLabel) { activeLabel.SetActive(true); }
        showRemoteVideo(uid);
    }
    // When remote user is offline, this delegate will be called. Delete user video object
    private void onUserOffline(uint uid, USER_OFFLINE_REASON reason) {
        Debug.Log("onUserOffline: uid = " + uid + " reason = " + reason);
        if(activeLabel) { activeLabel.SetActive(false); } 
        clearRemoteVideo();
    }

}