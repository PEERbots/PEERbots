using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

using CI.HttpClient;
using SimpleHTTP;

public class Misty : MonoBehaviour {

    //Misty Variables
    public string IP = "192.168.1.4";
    public InputField IPField;
    //LED Variables
    public int red, green, blue;
    //Image Variables
    public string imagename;
    //Head Variables
    public int headPitch, headRoll, headYaw;
    //Arm Variables
    public int leftArm, rightArm;
    //Track Variables
    public int left, right;
    public int linearVelocity, angularVelocity, timeMS;
    //Audio Variables
    public int volume;
    public string audioname;
    private string audiopath;
    private string SLASH;

    private string UID = "";

    public string ttsSpeech;
    public float ttsPitch = 1;
    public float ttsRate = 1;

    public bool sendAudioOnGenerationComplete = false;
    public AudioSource audiosource = null;

    public TextToSpeechSpeaker ttsSpeakerDropdown;   
    public Speaker speaker;

    public float audiosourceSentTime = 5;
    private float audiosourceSentTimer = 0;
    
    public bool sendOnAudio = true; //this is toggled by peerbotSender
    public bool isClipNull = true;

    public void Update() {
        //Try to send audio to Misty while RT Voice TTS is playing on Mac device or while using compatiblity voice
        if(sendOnAudio) {
            if(isMacOrCompatiblityVoice()) {
                if(audiosourceSentTimer > 0) { audiosourceSentTimer -= Time.deltaTime; }
                else if(audiosource.clip != null && isClipNull == true) {
                    audiosourceSentTimer = audiosourceSentTime;
                    SaveAudio(audioname); 
                }
                isClipNull = (audiosource.clip == null);
            }
        }
    }

    //--- Change IP Port Initialization ---//
    public void Start() {
        SLASH = (Application.platform == RuntimePlatform.Android ||
                 Application.platform == RuntimePlatform.OSXPlayer ||
                 Application.platform == RuntimePlatform.OSXEditor ||
                 Application.platform == RuntimePlatform.IPhonePlayer ||
                 Application.platform == RuntimePlatform.WindowsEditor ||
                 Application.platform == RuntimePlatform.WindowsPlayer)?"/":"\\";

        //get the UID for filenaming, first 100 chars
        UID = SystemInfo.deviceUniqueIdentifier;
        if(UID.Length > 100) { UID = UID.Substring(0, 100); }

        audiopath = Application.persistentDataPath + SLASH;
        Debug.Log("Local audio path: " + audiopath);

        IP = PlayerPrefs.GetString("UDPAddress", "127.0.0.1");
        if(IPField) { IPField.text = IP; }
    }
    public void setAddress(string address) { IP = address; PlayerPrefs.SetString("UDPAddress", address); }
    
    //--- Change LED ---// (red = 0-255 | green = 0-255 | blue = 0-255)
    //requests.post('http://'+self.ip+'/api/led',json={"Red": red,"Green": green,"Blue": blue})
    public void ChangeLED(int r, int g, int b) { red = r; green = g; blue = b;
        StartCoroutine(_ChangeLED (r,g,b) );
    }
	IEnumerator _ChangeLED(int r, int g, int b) {
        FormData formData = new FormData ()
            .AddField ("Red", ""+r)
            .AddField ("Green", ""+g)
            .AddField ("Blue", ""+b);
        Request request = new Request ("http://"+IP+"/api/led").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "ChangeLED(" + r + "," + g + "," + b +")");
	} 

    //--- Change Image ---//
    //Angry.jpg, Concerned.jpg, Confused.jpg, confused2.jpg, Content.jpg, Groggy.jpg, Happy.jpg, Love.jpg, Sad.jpg, Unamused.jpg, Waking.jpg;
    //requests.post('http://'+self.ip+'/api/images/display',json={'FileName': image_name ,'TimeOutSeconds': 5,'Alpha': 1})
    public void ChangeImage(string filename, int timeOutSeconds = 0, float alpha = 1) { imagename = filename;
        StartCoroutine(_ChangeImage(filename,timeOutSeconds,alpha));
    }
	IEnumerator _ChangeImage(string filename, int timeOutSeconds, float alpha) {
        FormData formData = new FormData ()
            .AddField ("Filename", filename)
            .AddField ("TimeOutSeconds", ""+timeOutSeconds)
            .AddField ("Alpha", ""+alpha);
        Request request = new Request ("http://"+IP+"/api/images/display").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult(http, "ChangeImage(" + filename + "," + timeOutSeconds + "," + alpha + ")");
	} 

    //--- Set Default Volume ---//
    //Endpoint: POST <robot-ip-address>/api/audio/volume
    public void SetDefaultVolume(int v) { volume = v;
        StartCoroutine(_SetDefaultVolume(v));
    }
	IEnumerator _SetDefaultVolume(int v) {
        FormData formData = new FormData ()
            .AddField ("Volume", ""+v);
        Request request = new Request ("http://"+IP+"/api/audio/volume").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "SetDefaultVolume(" + v + ")");
	} 

    //--- Play Audio ---//
    //requests.post('http://'+self.ip+'/api/audio/play',json={"AssetId": file_name})
    public void PlayAudio(string filename, int volume = 100) { audioname = filename;
        StartCoroutine(_PlayAudio(filename, volume));
    }
	IEnumerator _PlayAudio(string filename, int volume) {
        FormData formData = new FormData ()
            .AddField ("AssetId", filename)
            .AddField ("Volume", ""+volume);
        Request request = new Request ("http://"+IP+"/api/audio/play").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "PlayAudio(" + filename + "," + volume + ")");
	} 

    //--- Save Audio ---//
    //requests.post('http://'+self.ip+'/api/audio',json={"FileName": "tts.wav", "Data": "34,88,90,49,56,...", "ImmediatelyApply": True, "OverwriteExisting": True})
    public void SaveAudio(string filename, bool ImmediatelyApply = true, bool OverwriteExisting = true) { audioname = filename;
        //Mac & compatibility method: directly stream AudioClip to target
        if(isMacOrCompatiblityVoice()) {
            byte[] bytes = OpenWavParser.AudioClipToByteArray(audiosource.clip);
            string base64String = Convert.ToBase64String(bytes);           
            //Save data as text to see
            StreamWriter writer = new StreamWriter(audiopath + "tts.txt", false); //true to append, false to overwrite
            writer.WriteLine(base64String); 
            writer.Close();
            //Send it over!
            if(!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(base64String)) { 
                Debug.Log("[Here] Filename: " + filename + " | base64string: " + base64String);
                StartCoroutine(_SaveAudio(filename, base64String, ImmediatelyApply, OverwriteExisting));
            }
            else Debug.LogWarning("Null base64string! Cannot save audio.");
        } 
        //Android/Win/Linux method: save TTS first as WAV and then stream to target
        else {
            string path = audiopath + "tts" + ".wav"; //Speaker.AudioFileExtension;
            byte[] bytes = File.ReadAllBytes(path);
            string base64String = Convert.ToBase64String(bytes);
            //Save data as text to see
            StreamWriter writer = new StreamWriter(audiopath + "tts.txt", false); //true to append, false to overwrite
            writer.WriteLine(base64String); 
            writer.Close();
            //Send it over!
            if(!string.IsNullOrEmpty(filename) && !string.IsNullOrEmpty(base64String)) { 
                StartCoroutine(_SaveAudio(filename, base64String, ImmediatelyApply, OverwriteExisting));
            }
            else Debug.LogWarning("Null base64string! Cannot save audio.");
        }
    }
	IEnumerator _SaveAudio(string filename, string data, bool ImmediatelyApply, bool OverwriteExisting) {
        FormData formData = new FormData ()
            .AddField ("FileName", filename)
            .AddField ("Data", data)
            .AddField ("ImmediatelyApply", ImmediatelyApply.ToString())
            .AddField ("OverwriteExisting", OverwriteExisting.ToString());
        Request request = new Request ("http://"+IP+"/api/audio").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "SaveAudio(" + filename + "," + data + "," + ImmediatelyApply + "," + OverwriteExisting + ")" );
	} 
    
    //---Speak TTS (Misty Onboard TTS)---//
    public void SpeakTTS(string speech, float rate = 1, float pitch = 1, float volume = 1) { ttsSpeech = speech;
        if(speech != null && speech.Length > 0) { 
            //speech = "<speak>" + "<prosody volume="+(int)(volume*100)+">" + speech + "</prosody>" + "</speak>";
            //speech = "<speak> I can talk at different speeds. <prosody rate=\"fast\">I can talk really fast</prosody>. <prosody rate=\"x-slow\">Or I can talk really slow</prosody>. I can talk at different volumes. This is the default volume. <prosody volume=\"x-low\">I can whisper.</prosody>. <prosody volume=\"x-loud\">Or I can yell!</prosody> </speak>";
            //SimpleHTTPPost("http://"+IP+"/api/tts/speak", "{Text:\"" + speech + "\"}");
            //SimpleHTTPPost("http://"+IP+"/api/tts/speak", "{Text:\"" + speech + "\", Rate:"+(int)(rate*100)+", Pitch:"+(int)(pitch*100)+", Volume:"+(int)(volume*100)+"}");
            StartCoroutine( _SpeakTTS(speech, (int)(rate*100), (int)(pitch*100), (int)(volume*100)));
        }    
        Debug.Log("Sent speech (on-board TTS): " + speech);       
    }
    IEnumerator _SpeakTTS(string speech, int rate, int pitch, int volume) {
        //Get convert rate int to string label
        string r = "default"; 
        if(rate > 500) { r = "x-fast"; }
        else if(rate > 250) { r = "fast"; }
        else if(rate > 100) { r = "medium"; }
        else if(rate > 60) { r = "slow"; }
        else if(rate > 30) { r = "x-low"; }
        else { r = "x-low"; }
        //Get convert pitch int to string label
        string p = "default";
        if(pitch > 200) { p = "x-high"; }
        else if(pitch > 150) { p = "high"; }
        else if(pitch > 100) { p = "medium"; }
        else if(pitch > 75) { p = "low"; }
        else if(pitch > 50) { p = "x-low"; }
        else { p = "x-low"; }
        //Get convert volume int to string label
        string v = "default";
        if(volume > 450) { v = "x-loud"; }
        else if(volume > 300) { v = "loud"; }
        else if(volume > 120) { v = "medium"; }
        else if(volume > 70) { v = "low"; }
        else if(volume > 25) { v = "x-low"; }
        else { v = "silent"; }
        //Get SSML speech with rate, pitch, volume        
        string s = "<speak>" + 
                   "<prosody rate=\""+r+"\">"+
                   "<prosody pitch=\""+p+"\">"+
                   "<prosody volume=\""+v+"\">"+
                   speech+
                   "</prosody>"+
                   "</prosody>"+ 
                   "</prosody></speak>";

        Debug.Log(s);
        FormData formData = new FormData ()
            .AddField ("Text", s)
            .AddField ("Flush", "true");
        Request request = new Request ("http://"+IP+"/api/tts/speak").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "SpeakTTS(" + speech  + "," + rate  + "," + pitch + "," + volume + ")");
    }    

    public void EnableAudioService() {
        Debug.Log("Call sent to \"Enable Audio Service\" on Misty.");
        StartCoroutine(_EnableAudioService());
        //SpeakTTS("Audio Service Enabled.");
    }
    IEnumerator _EnableAudioService() {
        FormData formData = new FormData ();
        Request request = new Request ("http://"+IP+"/api/services/audio/enable").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "EnableAudioService()");
    }    

    public string getFilename(string speech) {
        string speechAlphaNumLower = Regex.Replace(speech,"[^A-Za-z0-9]","").ToLower();
        //If speech length if too great, halve it by removing every other character
        while(speechAlphaNumLower.Length > 32) { string halved = "";
            for(int i = 0; i < speechAlphaNumLower.Length; i +=2 ) { halved += speechAlphaNumLower[i]; }
            speechAlphaNumLower = halved;
        }
        string voiceAlphaNumLower = ((ttsSpeakerDropdown.getVoice() != null) ? Regex.Replace(ttsSpeakerDropdown.getVoice().Name,"[^A-Za-z0-9]","").ToLower() : "?");
        return speechAlphaNumLower + "_" + voiceAlphaNumLower + "_" + UID + ".wav"; //Speaker.AudioFileExtension;
    }

    //--- Say TTS (Unity Android Only) ---//
    //rate = 0-3, pitch = 0-2, volume = 0-1
    public void SayTTS(string speech, float rate = 1, float pitch = 1, bool speak = false) { ttsSpeech = speech; ttsRate = rate; ttsPitch = pitch;
        if(speech != null && speech.Length > 0) { 
            Debug.Log("Unique System Identifier: " + UID);
            audioname = getFilename(speech);
            PlayAudio(audioname); 
        }
    }
    //--- Save TTS (Unity Android Only) ---//
    public void SaveTTS(string speech, float rate = 1, float pitch = 1, bool speak = false) { ttsSpeech = speech; ttsRate = rate; ttsPitch = pitch;
        if(speech != null && speech.Length > 0) { 
            // Speak (string text, AudioSource source=null, Model.Voice voice=null, bool speakImmediately=true, float rate=1f, float pitch=1f, float volume=1f, string outputFile="", bool forceSSML=true)
            //Speaker.Speak(speech, null, null, speak, rate, pitch, 1, audiopath + "tts"); //rate = 0-3, pitch = 0-2, volume = 0-1

            float v = audiosource.volume; //save the volume, and mute it before generation
            audiosource.volume = 0;
            speaker.Speak(speech, audiosource, ttsSpeakerDropdown.getVoice(), speak, rate, pitch, 1, audiopath + "tts"); //rate = 0-3, pitch = 0-2, volume = 0-1
            audiosource.volume = v;

            audioname = getFilename(speech);
            Debug.Log("Audio file: " + audioname);
            
            if(!isMacOrCompatiblityVoice()) {
                sendAudioOnGenerationComplete = true;
            }
        }        
        Debug.Log("Saved speech to local device: " + speech);   
    }
    public void OnEnable() { speaker.OnSpeakAudioGenerationComplete += onSpeakAudioGenerationComplete; }
    public void OnDisable() { speaker.OnSpeakAudioGenerationComplete -= onSpeakAudioGenerationComplete; }
    private void onSpeakAudioGenerationComplete(Crosstales.RTVoice.Model.Wrapper wrapper) { Debug.Log("Speech generated: " + wrapper);
        //If not Mac, just send the autogenerated file as WAV
        if(sendAudioOnGenerationComplete && !isMacOrCompatiblityVoice()) { SaveAudio(audioname); }
    }

    //--- Move Head ---// (pitch = 20 to -40 | roll = -40 to 40 | yaw = -90 to 90)
    //requests.post('http://'+self.ip+'/api/head',json={"Pitch": pitch, "Roll": roll, "Yaw": yaw, "Velocity": velocity})
    public void MoveHead(int p, int r, int y, int v = 100) { headPitch = p; headRoll = r; headYaw = y;
        StartCoroutine(_MoveHead(p,r,y,v));
    }
    IEnumerator _MoveHead(int p, int r, int y, int v) {
        FormData formData = new FormData ()
            .AddField ("Pitch", ""+p)
            .AddField ("Roll", ""+r)
            .AddField ("Yaw", ""+y)
            .AddField ("velocity", ""+v);
        Request request = new Request ("http://"+IP+"/api/head").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "MoveHead(" + p + "," + r + "," + y + "," + v + ")");
    }   
    public void rotateHeadPitch(float angle) { 
        if(headPitch != (int)angle) { MoveHead(headPitch, headRoll, headYaw); headPitch = (int)angle; }   
    }
    public void rotateHeadRoll(float angle) {
        if(headRoll != (int)angle) { MoveHead(headPitch, headRoll, headYaw); headRoll = (int)angle; }
    }
    public void rotateHeadYaw(float angle) {
        if(headYaw != (int)angle) { MoveHead(headPitch, headRoll, headYaw); headYaw = (int)angle; }
    }
    
    //--- Move Arm ---// (leftArmPosition = -180 to 0 | leftArmPosition = -180 to 0)
    //requests.post('http://'+self.ip+'/api/head',json={"Pitch": pitch, "Roll": roll, "Yaw": yaw, "Velocity": velocity})
    public void MoveArms(int l, int r, int v = 50) { leftArm = l; rightArm = r;
        StartCoroutine(_MoveArms(l,r,v));
    }
    IEnumerator _MoveArms(int l, int r, int v) {
        FormData formData = new FormData ()
            .AddField ("LeftArmPosition", ""+l)
            .AddField ("RightArmPosition", ""+r)
            .AddField ("LeftArmVelocity", ""+v)
            .AddField ("RightArmVecloity", ""+v);
        Request request = new Request ("http://"+IP+"/api/arms/set").Post (RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "MoveArms(" + l + "," + r + "," + v + ")");
    }   

    //--- Drive ---// (left = -100 to 100 | right = -100 to 100)
    //requests.post('http://'+self.ip+'/api/drive/track',json={"LeftTrackSpeed": left_track_speed,"RightTrackSpeed": right_track_speed})
    public void DisableHazardSensors() {
        StartCoroutine(_DisableHazardSensors());
    }
    IEnumerator _DisableHazardSensors() {
        FormData formData = new FormData ()
            .AddField ("bumpSensorsEnabled", "[{\"sensorName\":\"Bump_FrontRight\",\"enabled\":false},{\"sensorName\":\"Bump_FrontLeft\",\"enabled\":false},{\"sensorName\":\"Bump_RearRight\",\"enabled\":false},{\"sensorName\":\"Bump_RearLeft\",\"enabled\":false}]")
            .AddField ("timeOfFlightThresholds", "[{\"sensorName\":\"TOF_DownFrontRight\",\"threshold\":0},{\"sensorName\":\"TOF_DownFrontLeft\",\"threshold\":0},{\"sensorName\":\"TOF_DownBackRight\",\"threshold\":0},{\"sensorName\":\"TOF_DownBackLeft\",\"threshold\":0},{\"sensorName\":\"TOF_Right\",\"threshold\":0},{\"sensorName\":\"TOF_Left\",\"threshold\":0},{\"sensorName\":\"TOF_Center\",\"threshold\":0},{\"sensorName\":\"TOF_Back\",\"threshold\":0}]");
        Request request = new Request ("http://"+IP+"/api/hazard/updatebasesettings").Post(RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "DisableHazardSensors()");
    }
    public void EnableHazardSensors() {
        StartCoroutine(_EnableHazardSensors());
    }
    IEnumerator _EnableHazardSensors() {
        FormData formData = new FormData ()
            .AddField ("bumpSensorsEnabled", "[{\"enabled\":true,\"sensorName\":\"Bump_FrontRight\"},{\"enabled\":true,\"sensorName\":\"Bump_FrontLeft\"},{\"enabled\":true,\"sensorName\":\"Bump_RearRight\"},{\"enabled\":true,\"sensorName\":\"Bump_RearLeft\"}]")
            .AddField ("timeOfFlightThresholds", "[{\"sensorName\":\"TOF_Right\",\"threshold\":0.215},{\"sensorName\":\"TOF_Center\",\"threshold\":0.215},{\"sensorName\":\"TOF_Left\",\"threshold\":0.215},{\"sensorName\":\"TOF_Back\",\"threshold\":0.215},{\"sensorName\":\"TOF_DownFrontRight\",\"threshold\":0.06},{\"sensorName\":\"TOF_DownFrontLeft\",\"threshold\":0.06},{\"sensorName\":\"TOF_DownBackRight\",\"threshold\":0.06},{\"sensorName\":\"TOF_DownBackLeft\",\"threshold\":0.06}]");
        Request request = new Request ("http://"+IP+"/api/hazard/updatebasesettings").Post(RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "ENableHazardSensors()");
    }

    public void Drive(int l, int a) { linearVelocity = l; angularVelocity = a;  
        StartCoroutine(_Drive(l ,a));
    }
    IEnumerator _Drive(int l, int a) {
        FormData formData = new FormData ()
            .AddField ("LinearVelocity", ""+l)
            .AddField ("AngularVelocity", ""+a);
        Request request = new Request ("http://"+IP+"/api/drive").Post(RequestBody.From(formData));
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "Drive(" + l + "," + a + ")");
    }   
    
    //--- Drive Track ---// (left = -100 to 100 | right = -100 to 100)
    //requests.post('http://'+self.ip+'/api/drive/track',json={"LeftTrackSpeed": left_track_speed,"RightTrackSpeed": right_track_speed})
    //public void DriveTrack(int l, int r) { left = l; right = r;    
        //SimpleHTTPPost("http://"+IP+"/api/drive/track", "{LeftTrackSpeed:\""+left+"\",RightTrackSpeed:\""+right+"\"}");
    //}

    //--- Drive Time ---// (left = -100 to 100 | right = -100 to 100)
    //requests.post('http://'+self.ip+'/api/drive/track',json={"LeftTrackSpeed": left_track_speed,"RightTrackSpeed": right_track_speed})
    //public void DriveTime(int l, int a, int t) { linearVelocity = l; angularVelocity = a; timeMS = t;  
        //SimpleHTTPPost("http://"+IP+"/api/drive/time", "{LinearVelocity:\""+linearVelocity+"\",AngularVelocity:\""+angularVelocity+"\", TimeMS:\""+timeMS+"\"}");
    //}
    
    //--- Stop ---//
    //requests.post('http://'+self.ip+'/api/drive/stop')
    public void Stop() {   
        StartCoroutine(_Stop());
    }
    IEnumerator _Stop() {
        FormData formData = new FormData ()
            .AddField ("Hold", "false");
        Request request = new Request ("http://"+IP+"/api/drive/stop").Post(RequestBody.From(formData));
        Client http = new Client (); yield return http.Send(request); ProcessResult (http, "Stop()");
    } 

    /*
    //Halt works better than stop?
    public void Halt() {   
        StartCoroutine(_Halt());
    }
    IEnumerator _Halt() {
        Request request = new Request ("http://"+IP+"/api/halt").Post(RequestBody.From(" "));
		Client http = new Client (); yield return http.Send(request); ProcessResult (http, "Halt()");
    } 
    */

    /*
    //Clayton Industries HTTP API quick helper function
    public void newHTTPClient(string URI, string json) {
        if(json != null && json.Length > 0) { Debug.Log("JSON sent: " + json); }    
        new HttpClient().Post(new System.Uri(URI), new StringContent(json), HttpCompletionOption.AllResponseContent, (response) => {
            //#pragma warning disable 0219
            if(response != null) { 
                string responseData = (response != null) ? response.ReadAsString() : "No response from HTTP target!"; 
                //string responseData = response.ReadAsString(); 
                Debug.Log("HTTP Response: " + responseData);
                //If we get an callback denoting upload successful, play it: (Because "ImmediatelyApply" doesn't work)
                //if(responseData == "{\"result\":[{\"name\":\"tts.wav\",\"systemAsset\":false}],\"status\":\"Success\"}") {
                //if(responseData.Length > 47 && responseData.Substring(responseData.Length - 47) == ".wav\",\"systemAsset\":false}],\"status\":\"Success\"}") { PlayAudio(audioname); }
                //if(responseData == {\"error\":\"Object reference not set to an instance of an object.\",\"status\":\"Failed\"}" ) { PlayAudio(audioname); }
                //IF we can't find TTS file pre-loaded onto Misty, make a new one and send it over.
                if(responseData == "{\"error\":\"Unable to find requested audio clip.\",\"status\":\"Failed\"}") { SaveTTS(ttsSpeech); }

                //For Misty Onboard TTS, enable Audio Service if it is disabled
                if(responseData == "{\"error\":\"Audio Service must be enabled for Text-to-Speech\",\"status\":\"Failed\"}" ||
                   responseData == "{\"error\":\"Audio Service must be enabled for Text-to-Speech.\",\"status\":\"Failed\"}" ||
                   responseData == "{\"error\":\"You must enable the audio service to execute this method.\",\"status\":\"Failed\"}") { EnableAudioService(); }
            }
            //#pragma warning restore 0219
        });   
    }
    */

    //Simple HTTP Response Handler
	void ProcessResult(Client http, string function) {
		if (http.IsSuccessful ()) {
			Response resp = http.Response ();
			Debug.Log("Function: " + function + " | HTTP success: " + resp.Status().ToString() + "\nbody: " + resp.Body());

            //IF we can't find TTS file pre-loaded onto Misty, make a new one and send it over.
            string responseData = resp.Body();
            if(responseData == "{\"error\":\"Unable to find requested audio clip.\",\"status\":\"Failed\"}") { SaveTTS(ttsSpeech, ttsRate, ttsPitch); }

            //For Misty Onboard TTS, enable Audio Service if it is disabled
            if(responseData == "{\"error\":\"Audio Service must be enabled for Text-to-Speech\",\"status\":\"Failed\"}" ||
                responseData == "{\"error\":\"Audio Service must be enabled for Text-to-Speech.\",\"status\":\"Failed\"}" ||
                responseData == "{\"error\":\"You must enable the audio service to execute this method.\",\"status\":\"Failed\"}") { EnableAudioService(); }

		} else {
			Debug.LogWarning("HTTP error: " + http.Error());
		}
	}

    //----QUICK HELPER CHECK FUNCTIONS---//
    public bool isMacOrCompatiblityVoice(){
        //Check null
        if(audiosource == null) { Debug.LogWarning("AudioSource is null! Please assign Audio Source."); return false; }
        //Check if using compatibility voices 
        if(ttsSpeakerDropdown.useCompatiblityVoices) {return true;}
        //Check if using iOS or Cac
        return (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.OSXEditor);
    }

}