using UnityEngine;
using UnityWebServer;

[UnityHttpServer]
public class PEERbotHTTPServer : MonoBehaviour {

    public FaceMasterController face;

    //------------SERVER------------//
    [UnityHttpRoute("/SetBehaviour", "POST")]
    public void SetBehaviour(HttpRequest request, HttpResponse response) { 
        //Attempt to parse JSON body
        string json = request.BodyText;

        bool success = face.processJSON(json);
        
        //Send success or failure response
        if(success) { response.BodyText = "{\"SUCCESS\": \"Behaviour JSON received.\"}"; } 
        else { response.BodyText = "{\"FAILURE\": \"Behaviour JSON could not be parsed.\"}"; }
        
        Debug.Log(response.BodyText);
    }

    [UnityHttpRoute("/Blink", "GET")]
    public void SetBlink(HttpRequest request, HttpResponse response) { 
        face.blink(); 
        //Send success or failure response
        response.BodyText = "{\"SUCCESS\": \"Blink Command received.\"}";

        Debug.Log(response.BodyText);
    }

}
