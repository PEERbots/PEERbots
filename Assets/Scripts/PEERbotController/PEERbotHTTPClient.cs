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

using SimpleHTTP;

public class PEERbotHTTPClient : MonoBehaviour {

    public string IP = "192.168.1.4";
    public int port = 8000;
    
    public InputField IPField;
    public InputField PortField;
    
    //Set IP and Port UI Fields
    void Start() {
        IP = PlayerPrefs.GetString("HTTPSendAddress", "127.0.0.1");
        port = PlayerPrefs.GetInt("HTTPSendPort", 8000);
        if(IPField) { IPField.text = IP; } if(PortField) { PortField.text = "" + port; }
    }
    public void setAddress(string address) { IP = address; PlayerPrefs.SetString("HTTPSendAddress", address); }
    public void setPort(int value) { port = value; PlayerPrefs.SetInt("HTTPSendPort", port); }
    public void setPort(string text) { 
        if(!int.TryParse(text, out port)) { port = 8000; } 
        PlayerPrefs.SetInt("HTTPSendPort", port);
    }

    //Send a formatted behaviour JSON
    public void SendBehaviour(PEERbotButtonDataFull data) {
        StartCoroutine(_SendBehaviour(data) );
    }
	IEnumerator _SendBehaviour(PEERbotButtonDataFull data) {
        //Create an HTTP request
        Request request = new Request ("http://"+IP+":"+port+"/SetBehaviour")
            .Post (RequestBody.From<PEERbotButtonDataFull> (data));
        //And send it
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "SendBehaviour");
	} 

    //Send a simple blink
    public void SendBlink() {
        StartCoroutine(_SendBlink() );
    }
	IEnumerator _SendBlink() {
        //Create an HTTP request
        Request request = new Request ("http://"+IP+":"+port+"/Blink");
        //And send it
		Client http = new Client (); yield return http.Send(request); 
        ProcessResult (http, "SendBlink");
	} 

    //Simple HTTP Response Handler
	void ProcessResult(Client http, string function) {
		if (http.IsSuccessful ()) {
			Response resp = http.Response ();
			Debug.Log("Function: " + function + " | HTTP success: " + resp.Status().ToString() + "\nbody: " + resp.Body());
		} else {
			Debug.LogWarning("HTTP error: " + http.Error());
		}
	}

}