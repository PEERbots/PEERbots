using UnityEngine; 
using System.Collections.Generic;

public enum ExpressionMode { BLENDSHAPE, TEXTURE };
    
public class AdvancedFace : MonoBehaviour {

    public ExpressionMode expressionMode = ExpressionMode.BLENDSHAPE; 
    public int expression = 0;

    [Header("Blendshape Expressions")]
    public float blendSpeed = 1f;
    private int previousExpression = 0;
    
    [Header("Texture Expressions")]
    public List<TextureExpression> textureExpressions;
    
    [Header("Face Color")]
    public Color faceColor = new Color(0.4f, 1, 1);
    private Color currentFaceColor = new Color(0.4f, 1, 1);

    [Header("Blink Variables")]
    public float blinkSpeed = 4f;
    public float blinkDelay = 4f;
    private float blinkSize = 1f;
    private float blinkTimer = 1f;
    
    [Header("LipSync Variables")]
    public float gain = 100;
    public float lipMult = 1;
    public float lipSpeed = 1;
    private float lipPitch = 0;
    public float lipStretch = 1;
    public float lipLerpSpeed = 1;
    public enum LipMode { migo, hugo };
    public LipMode lipMode = LipMode.migo;
    public Vector3 mouthScale;

    [Header("GameObject Assignments")]
    public GameObject face;
    public LineRenderer line;
    public EyeWarp leftEyeWarp;
    public EyeWarp rightEyeWarp;
    public MeshRenderer textureMouth;
    public SkinnedMeshRenderer leftEye;
    public SkinnedMeshRenderer rightEye;
    
    public TextToSpeechSpeaker speaker;
    
    [Header("Color Settings")]
    public PEERbotMappings mappings;
    
    // Update is called once per frame
    void Update() {
        updateExpression();
        updateFaceColor();
        updateLipSync();
        updateBlinking();
    }

    //set blink data
    public void blink() { blinkTimer = 0; }
    private void updateBlinking() {
        if (blinkTimer > 0) {
            blinkTimer -= Time.deltaTime;
        } else {
            blinkTimer = UnityEngine.Random.Range(blinkDelay/2, blinkDelay);
            blinkSize = -1;
        }
        blinkSize = Mathf.Lerp(blinkSize,1,Time.deltaTime * blinkSpeed);
        if(leftEyeWarp) leftEyeWarp.blink = blinkSize;
        if(rightEyeWarp) rightEyeWarp.blink = blinkSize;
    }
    //control face
    private void updateLipSync() {
        float A = 0;
        if(Application.platform == RuntimePlatform.WebGLPlayer ||
           Application.platform == RuntimePlatform.IPhonePlayer) {
            if(!speaker.doneSpeaking) { A = Mathf.Sin(Time.time * lipSpeed) * lipMult * 0.2f; }
        } else {
            //Get audio analysis
            float[] spectrum = new float[64];
            AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);
            //Get sum of audio analysis
            for (int i = 0; i < 64; i++) { A += spectrum[i] * gain; }
            A /= 64;
        }
        lipPitch = Mathf.Lerp(lipPitch, A, Time.deltaTime * lipLerpSpeed);
        
        //Line Style Mouth
        if (line != null) {
            //Show FFT on line renderers
            if (lipMode == LipMode.migo) {
                for (int i = 0; i < line.positionCount; i++) {
                    float pos = Mathf.Sin(Time.time * lipSpeed + i * lipStretch) * lipPitch * lipMult;
                    line.SetPosition(i, new Vector3(i - line.positionCount / 2, pos, 0));
                }
            }
            if (lipMode == LipMode.hugo) {
                line.transform.localScale = new Vector3(line.transform.localScale.x, lipPitch * lipMult, line.transform.localScale.z);
            }
        }
        //Texture Style Mouth
        if(textureMouth != null) {
            textureMouth.transform.localScale = new Vector3(mouthScale.x, mouthScale.y + lipPitch * lipMult, mouthScale.z);   
        }
    }

    //controls face color
    public void setFaceColor(Color color) { faceColor = color; }
    private void updateFaceColor() {
        currentFaceColor = Color.Lerp(currentFaceColor, faceColor, Time.deltaTime * blendSpeed);
        face?.GetComponent<Renderer>().material.SetColor("_Color", currentFaceColor);
    }

    //controls face expression
    public void setExpression(int e) { expression = e; }
    private void updateExpression() {
        if(expressionMode == ExpressionMode.BLENDSHAPE) {
            //For each emotion, if current expression, set to weight to 100, else 0 for both eyes
            foreach(EmotionMap map in mappings.emotions) {
                if(leftEye && map.leftEyeShape >= 0) { //negative eyeShape means no blendshape applied
                    float leftWeight = Mathf.Lerp(leftEye.GetBlendShapeWeight(map.leftEyeShape), (expression == map.expression) ? 100 : 0, Time.deltaTime * blendSpeed);
                    leftEye.SetBlendShapeWeight(map.leftEyeShape, leftWeight);
                }
                if(rightEye && map.rightEyeShape >= 0) {
                    float rightWeight = Mathf.Lerp(rightEye.GetBlendShapeWeight(map.rightEyeShape), (expression == map.expression) ? 100 : 0, Time.deltaTime * blendSpeed);
                    rightEye.SetBlendShapeWeight(map.rightEyeShape, rightWeight);
                }
            } 
        } else if(expressionMode == ExpressionMode.TEXTURE) {
            switch (expression) {
                case 0: setTextureExpression(0); break; //Neutral;
                case 1: setTextureExpression(1); break; //Surprised; 
                case 2: setTextureExpression(2); break; //Happy; 
                case 3: setTextureExpression(3); break; //Sad
                case 4: setTextureExpression(4); break; //Concerned
                case 5: setTextureExpression(5); break; //Sleep
                default: setTextureExpression(0); break; //Neutral
            }
        }
        
    }
    private void setTextureExpression(int index) {
        if(!(0 <= index && index < textureExpressions.Count)) { Debug.LogWarning("Bad Texture Index out of range  at [" + index + "]!"); return; }
        
        if(leftEye && textureExpressions[index].eyeLeft) { leftEye.material.mainTexture = textureExpressions[index].eyeLeft; }
        if(rightEye && textureExpressions[index].eyeRight) { rightEye.material.mainTexture = textureExpressions[index].eyeRight; }
        if(textureMouth && textureExpressions[index].mouth) { textureMouth.material.mainTexture = textureExpressions[index].mouth; }
        
        if(leftEyeWarp) { leftEyeWarp.setSize(textureExpressions[index].eyeScale); }
        if(rightEyeWarp) { rightEyeWarp.setSize(textureExpressions[index].eyeScale); }
        
        mouthScale = textureExpressions[index].mouthScale;
    
    }

}

