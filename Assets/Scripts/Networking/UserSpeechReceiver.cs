using UnityEngine;
using UnityEngine.UI;

using System;

public class UserSpeechReceiver : MonoBehaviour {

	public EasyAgoraController agoraController;

	public Text usernameText;
	public Text speechText;

	// Update is called once per frame
	void Update() {
        //Poll for latest message from json Receiver
        if(agoraController != null) {
            string message = agoraController.getMessage();
            if (message != null && message.Length > 0) { processJSON(message); }
        }
    }

    /// Process json:
    public void processJSON(string jsonStr) {
        Debug.Log("Received: " + jsonStr);
        //Debug.Log("Received request to parse message. Attempting to parse...");
        try { UserSpeech userSpeech = JsonUtility.FromJson<UserSpeech>(jsonStr);
            processUserSpeech(userSpeech);
        } catch (Exception e) { Debug.LogError("JSON Parse failed. Here is the failed string: " + jsonStr); }
    }
	/// Process UserSpeech
    public void processUserSpeech(UserSpeech userSpeech) {
        if (userSpeech != null) {
        	usernameText.text = userSpeech.username; 
        	speechText.text = userSpeech.speech;
        }
    }

}

[System.Serializable]
public class UserSpeech {
	public string username;
	public string speech;
}
