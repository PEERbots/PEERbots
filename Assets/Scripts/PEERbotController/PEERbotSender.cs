using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

//public enum SendMode { NONE, HTTP, MISTY, UDP, BLUETOOTH, AGORA, WEBRTC, SERIAL };
public enum SendMode { NONE, HTTP, MISTY, AGORA };
    
public class PEERbotSender : MonoBehaviour {

    [Header("PEERbot Controller Connections")]
    public PEERbotButtonEditorUI editorUI;
    public PEERbotMappings mappings;
    public PEERbotController pc;
    public PEERbotLogger logger;

    [Header("Sender Mode")]
    public SendMode sendMode;
    public Dropdown sendModeDropdown;
    
    public PEERbotHTTPClient httpSender;
    public EasyAgoraController agora;
    public UDPSender udpSender;
    public Misty misty; 
    
    public GameObject bluetoothPanel;
    public GameObject udpPanel;
    public GameObject agoraPanel;
    public GameObject mistyPanel;
    public GameObject ipPanel;

    [Header("Misc Speech Variables")]
    public TextToSpeechSpeaker tts;
    public Toggle localTTSToggle; public bool useLocalTTS = false;
    public Toggle mistyTTSToggle; public bool useMistyTTS = false;
    private string quickSpeech = "";

    ///---------------------------------------------///
    ///----------INITIALIZATION FUNCTIONS-----------///
    ///---------------------------------------------///

    //Initializing Palettes.
    void Start() {
        if(Application.platform != RuntimePlatform.WebGLPlayer) { initSendMode(); }

        //Set Local TTS and Misty TTS preferences
        SetLocalTTS(PlayerPrefs.GetInt("LocalTTS", 0)>0?true:false);
        SetMistyTTS(PlayerPrefs.GetInt("MistyTTS", 0)>0?true:false);
    }
    //Initializing Send Mode.
    public void setSendMode(int mode) {
        sendMode = (SendMode)mode;
        PlayerPrefs.SetInt("SendMode", mode);

        //Bluetooth specific
        //bluetoothPanel.SetActive(sendMode == SendMode.BLUETOOTH);
        
        //UDP Specific
        //udpPanel.SetActive(sendMode == SendMode.UDP);
        //if(sendMode != SendMode.UDP) { udpSender.stopSenderThread(); }
        
        //Agora Specific
        agoraPanel.SetActive(sendMode == SendMode.AGORA); 
        if(sendMode != SendMode.AGORA) { agora.leave(); }
        
        //Misty specific
        mistyPanel.SetActive(sendMode == SendMode.MISTY);
        
        //Misty or UDP
        ipPanel.SetActive(/*sendMode == SendMode.UDP ||*/ sendMode == SendMode.MISTY || sendMode == SendMode.HTTP);
    }
    public void initSendMode() { 
        if(sendModeDropdown) { 
            //Clear Dropdown
            sendModeDropdown.ClearOptions();
            //Add Enums to dropdown
            sendModeDropdown.AddOptions( SendMode.GetNames(typeof(SendMode)).ToList() );
            //Set Send Mode from Memory
            sendModeDropdown.value = PlayerPrefs.GetInt("SendMode", 0); 
        }    
        SetLocalTTS(PlayerPrefs.GetInt("LocalTTS", 0)>0?true:false);
        SetMistyTTS(PlayerPrefs.GetInt("MistyTTS", 0)>0?true:false);
    }

    ///--------------------------------------------------///
    ///---------------QUICK TTS FUNCTIONS----------------///
    ///--------------------------------------------------///
    //TTS Type Toggle
    public void SetLocalTTS(bool isOn) { useLocalTTS = isOn;
        localTTSToggle.isOn  = isOn; PlayerPrefs.SetInt("LocalTTS", isOn?1:0);
        AudioListener.volume = isOn ? 1 : 0;
        misty.sendOnAudio = !isOn;
    }
    public void SetMistyTTS(bool isOn) { useMistyTTS = isOn;
        mistyTTSToggle.isOn  = isOn; PlayerPrefs.SetInt("MistyTTS", isOn?1:0);
    }
    //Say TTS locally
    public void sayTTS() { sayTTS(editorUI.speechField.text); }
    public void sayTTS(string speech) {
        if(speech != null && speech.Length > 0) {
            misty.sendAudioOnGenerationComplete = false;
            tts.sayTTS(speech, editorUI.volumeSlider.value, editorUI.rateSlider.value, editorUI.pitchSlider.value); 
        }                
    }
    //Quick Misty Control Buttons
    public void sayMistyTTS() { sayMistyTTS(editorUI.speechField.text); }
    public void sayMistyTTS(string speech) {
        if(speech != null && speech.Length > 0) {
            if(useMistyTTS) {
                misty.sendAudioOnGenerationComplete = false;
                misty.SpeakTTS(speech, editorUI.rateSlider.value, editorUI.pitchSlider.value, editorUI.volumeSlider.value); 
            } else {
                misty.sendAudioOnGenerationComplete = true;
                misty.SayTTS(speech, editorUI.rateSlider.value, editorUI.pitchSlider.value, true);
            }
        }            
    }
    public void saveMistyTTS() { saveMistyTTS(editorUI.speechField.text); }
    public void saveMistyTTS(string speech) {
        if(speech != null && speech.Length > 0) {
            misty.sendAudioOnGenerationComplete = true;
            misty.SaveTTS(speech, editorUI.rateSlider.value, editorUI.pitchSlider.value, false);
        }                
    }
    //Quick Speech
    public void setQuickSpeech(string text) { 
        if(!string.IsNullOrEmpty(text)) { quickSpeech = text; } 
    }
    public void sendQuickSpeech() {
        if(string.IsNullOrEmpty(quickSpeech)) { return; }
        PEERbotButtonDataFull data = new PEERbotButtonDataFull();        
        data.title = "Quick Speech";
        //Speech vars
        data.speech = quickSpeech; 
        data.volume = editorUI.volumeSlider.value;
        data.rate = editorUI.volumeSlider.value;
        data.pitch = editorUI.volumeSlider.value;
        //Button vars
        data.emotion = mappings.emotions[editorUI.emotionDropdown.value].defaultEmotion;
        data.color = mappings.colors[editorUI.buttonColor.value].name;
        data.r = mappings.colors[editorUI.buttonColor.value].color.r;
        data.g = mappings.colors[editorUI.buttonColor.value].color.g;
        data.b = mappings.colors[editorUI.buttonColor.value].color.b;
        //Gaze Vars
        data.gazeX = editorUI.gazeXSlider.value;
        data.gazeY = editorUI.gazeYSlider.value;
        //Send the message
        sendMessage(data);
        //Log it
        if(pc.currentPalette != null) { logger.AddToLog(pc.currentPalette, data); }
    }

    ///----------------------------------------------///
    ///--------------SENDING FUNCTIONS---------------///
    ///----------------------------------------------///

    //---SEND MESSAGE---//
    public void sendMessage() { 
        //Try to the current button if exist
        if(pc.currentButton != null) { sendMessage(pc.currentButton); } 
        //Otherwise just send whatever is in the UI
        //else { sendQuickSpeech(editorUI.speechField.text); }
    }
    public void sendMessage(PEERbotButton button) { if(button == null) { Debug.LogWarning("Button is null! Cannot send button."); return; } 
        sendMessage(button.data);
    }
    public void sendMessage(PEERbotButtonDataFull data) {
        if(useLocalTTS) { sayTTS(data.speech); } //For saying stuff on controller device, and not sending it over
        switch(sendMode) { 
            case SendMode.AGORA: sendAgoraMessage(data); break;
            case SendMode.MISTY: sendMistyMessage(data); break;
            case SendMode.HTTP: sendHTTPMessage(data); break;
            //case SendMode.UDP: sendUDPMessage(data); break;
        }
    }
    //UDP / HTTP / Bluetooth / WebRTC / Agora Messages
    private void sendUDPMessage(PEERbotButtonDataFull data) { udpSender?.sendMessage(JsonUtility.ToJson(data)); }
    private void sendAgoraMessage(PEERbotButtonDataFull data) { agora?.sendMessage(JsonUtility.ToJson(data)); }
    private void sendHTTPMessage(PEERbotButtonDataFull data) { httpSender?.SendBehaviour(data); }
    //Misty Messages
    public void sendMistyMessage(PEERbotButtonDataFull data) { 
        misty.ChangeLED(data.r, data.g, data.b);
        misty.MoveHead(misty.headPitch + UnityEngine.Random.Range(-5,5), misty.headRoll + UnityEngine.Random.Range(-5,5), misty.headYaw + UnityEngine.Random.Range(-5,5));
        misty.MoveArms(UnityEngine.Random.Range(-90,90),UnityEngine.Random.Range(-90,90)); 
        misty.SetDefaultVolume((int)(data.volume*100f));
        misty.ChangeImage(mappings.emotions[mappings.getEmotionIndexFromString(data.emotion)].mistyEmotion);
        if(!useLocalTTS) { sayMistyTTS(); }
    }

    //---SEND BLINK---//
    public void sendBlink() {
        switch(sendMode) { 
            case SendMode.AGORA: sendAgoraBlink(); break;
            case SendMode.MISTY: sendMistyBlink(); break;
            case SendMode.HTTP: sendHTTPBlink(); break;
            //case SendMode.UDP: sendUDPBlink(); break;
        }        
    }
    public void sendUDPBlink() { udpSender?.sendMessage("blink"); }
    public void sendAgoraBlink() { agora?.sendMessage("blink"); }
    public void sendHTTPBlink() { httpSender?.SendBlink(); }  
    //Misty Blink
    private void openMistyEyes() { misty.ChangeImage("e_DefaultContent.jpg"); }
    public void sendMistyBlink() {
        misty.ChangeImage("e_SystemBlinkStandard.jpg"); 
        Invoke("openMistyEyes",0.5f);
        misty.MoveHead(-10, 0, 0);
    }    

}
