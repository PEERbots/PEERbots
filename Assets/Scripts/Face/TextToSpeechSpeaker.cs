using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;

public class TextToSpeechSpeaker : MonoBehaviour {

    public bool speakOnStart = false;
    public bool doneSpeaking = true;
    public AudioSource audio;
    public string speech;
    
    //RTVoice TTS Elements
    public Dropdown voiceDropdown;
    private Voice TTSVoice = null;
    public Text subtitles;

    public Speaker speaker;
    public bool useCompatiblityVoices = false;

    // Start is called before the first frame update
    void Start() {
        speaker.CustomMode = (Application.platform == RuntimePlatform.WebGLPlayer);
        speaker.OnSpeakStart += SpeakStart;
        speaker.OnSpeakComplete += SpeakComplete;        
        speaker.OnVoicesReady += initVoiceDropdown;

        if(speaker.Voices != null) { initVoiceDropdown(); }

        if(speakOnStart) { sayTTS(speech); }
    }

    public float getAmplitude(int samples = 64, float gain = 100) {
        //Get Spectrum
        float[] spectrum = new float[samples];
        audio.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
        //Get average amplitude
        float amplitude = 0;
        for (int i = 0; i < samples; i++) { amplitude += spectrum[i] * gain; }
        return amplitude / samples;
    }

    void OnDestroy() {
        speaker.OnSpeakStart -= SpeakStart;
        speaker.OnSpeakComplete -= SpeakComplete;
    }
    private void SpeakStart(Wrapper wrapper) { 
        doneSpeaking = false; //Debug.Log("Speak start."); 
    }
    private void SpeakComplete(Wrapper wrapper) { 
        doneSpeaking = true; //Debug.Log("Speak Complete."); 
    }

    public void sayTTS(string speech) { sayTTS(speech, 1f, 1f, 1f, null); }
    public void sayTTS(string speech, float volume = 1f, float rate = 1f, float pitch = 1f, string voice = null) { //rate = 0-3, pitch = 0-2, volume = 0-1
        if(!String.IsNullOrEmpty(speech)) { 
            //Check if Audio Source is attached:
            if(gameObject.GetComponent<AudioSource>() != null) { gameObject.GetComponent<AudioSource>().volume = volume; }
            //if voice is supplied, use it
            if(voice != null) { 
                Voice voiceFromName = speaker.VoiceForName(voice);
                speaker.Speak(speech, audio, (voiceFromName != null) ? voiceFromName : TTSVoice, true, rate, pitch, 1); 
                if(voiceFromName != null) { TTSVoice = voiceFromName; } //Set speaker's main voice if found.
            } 
            //Otherwise use default voice
            else {
                speaker.Speak(speech, audio, TTSVoice, true, rate, pitch, 1); 
            }
            if(subtitles != null) { subtitles.text = speech; }
        }                
    }
    public void stopTTS() { speaker.Silence(); doneSpeaking = true; }
    
    //Get all voices available on device
    public void initVoiceDropdown() {
        if(voiceDropdown != null) {    
            //Add Voices to dropdown
            voiceDropdown.ClearOptions();
            List<string> dropOptions = new List<string>();
            foreach (Voice voice in speaker.Voices) { dropOptions.Add(voice.Name); }
            dropOptions.Add(useCompatiblityVoices ? "Use native voices" : "Use compatibility voices");
            voiceDropdown.AddOptions(dropOptions);
            //Use preferred voice if available
            int v = PlayerPrefs.GetInt("voice", 0);
            if (dropOptions.Count > 1) { selectVoice( (dropOptions.Count > v) ? v : 0); }
        }
    }

    public void selectVoice(int index) {
        if(index == voiceDropdown.options.Count - 1) {
            useCompatiblityVoices = !useCompatiblityVoices;
            Debug.Log(useCompatiblityVoices ? "Switched to compatibility voices." : "Switched to native voices.");
            speaker.ReloadProvider(useCompatiblityVoices);
            PlayerPrefs.SetInt("voice", 0);
            return;
        }
        PlayerPrefs.SetInt("voice", index);
        TTSVoice = speaker.Voices[index];
        voiceDropdown.value = index;
    
    }

    public Voice getVoice() {
        return TTSVoice;
    }
}
