using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageComparison: MonoBehaviour {
    private String _studentPath = "/Students/";
    private String _samplePath = "/Samples/";
    private String _canvas = "Canvas/";
    private String _currentFileName = "";
    private Dictionary<String, Color32[]> _sampleToColor;
    private Dictionary<String, Color32[]> _studentToColor;
    private Dictionary<String, Color32[]> _studentToDiff;
    public GameObject studentImage;
    public GameObject sampleImage;
    public GameObject sceneList;
    private bool PASS = true;
    private bool ISDIFFON = true;

    private void Start() {
        _sampleToColor = new Dictionary<string, Color32[]>();
        _studentToColor = new Dictionary<string, Color32[]>();
        _studentToDiff = new Dictionary<string, Color32[]>();
        StartCoroutine(ImagesDifferences());
    }

    private IEnumerator ImagesDifferences() {
        List<double> diff = new List<double>();
        // Get filenames as a list
        var studentPath = Application.dataPath + _studentPath;
        var studentFilePaths = Directory.GetFiles(studentPath);
        List<String> studentFileNames = new List<string>();
        // Get filenames as a list
        var samplePath = Application.dataPath + _samplePath;
        var sampleFilePaths = Directory.GetFiles(samplePath);
        List<String> sampleFileNames = new List<string>();
        
        foreach (var studentFilePath in studentFilePaths) {
            Debug.Log(studentFilePath);
            if (studentFilePath.EndsWith(".png"))
                studentFileNames.Add(Path.GetFileNameWithoutExtension(studentFilePath));
        }
        foreach (var sampleFilePath in sampleFilePaths) {
            if (sampleFilePath.EndsWith(".png"))
                sampleFileNames.Add(Path.GetFileNameWithoutExtension(sampleFilePath));
        }
        // Go through each file name
        GameObject buttonProto = GameObject.Find("ButtonProto");
        float curHeight = 0;
        foreach (var sampleFileName in sampleFileNames) {
            // Get TMP_Text component from scene

            GameObject textComp = new GameObject(sampleFileName + "Txt");
            TMP_Text sceneText = textComp.AddComponent<TextMeshProUGUI>();
            // sceneText.text = sampleFileName + "....................";
            sceneText.rectTransform.pivot = new Vector2(0.5f, 1);
            sceneText.rectTransform.anchorMin = new Vector2(0, 1);
            sceneText.rectTransform.anchorMax = new Vector2(1, 1);
            sceneText.rectTransform.offsetMax = new Vector2(0, 0);
            sceneText.rectTransform.offsetMin = new Vector2(0, sceneText.rectTransform.offsetMin.y);
            sceneText.rectTransform.position = new Vector3(0, -curHeight, 0);
            sceneText.fontSize = 28;
            curHeight += sceneText.rectTransform.rect.height;

            textComp.transform.SetParent(sceneList.transform, false);

            GameObject buttonGo = Instantiate(buttonProto);
            buttonGo.SetActive(true);
            Button button = buttonGo.GetComponent<Button>();
            button.GetComponent<RectTransform>().position = new Vector3(0, 0);
            button.onClick.AddListener(() => SetTexture(sampleFileName));

            buttonGo.transform.SetParent(textComp.transform, false);

            // if (GameObject.Find(_canvas + sampleFileName) == null) continue;
            
            // TMP_Text sceneText = GameObject.Find(_canvas + sampleFileName).GetComponent<TMP_Text>();
          
            // Check if student file exists
            if (!studentFileNames.Contains(sampleFileName)) {
                PASS = false;
                Debug.Log(sampleFileName + " Not Existed");
                sceneText.text = sampleFileName + " (N/A)";
                sceneText.color = Color.gray;
            }
            else {
                // Get Button component from scene
                // Button button = GameObject.Find(_canvas + sampleFileName + "/" + sampleFileName + "Button").GetComponent<Button>();
                button.gameObject.SetActive(true);
                diff.Add(0);    
                Texture2D sampleTex = new Texture2D(2, 2);
                sampleTex.LoadImage(ImageToByteArray(_samplePath, sampleFileName));
                Color32[] sampleImageColors = sampleTex.GetPixels32();
                Texture2D studentTex = new Texture2D(2, 2);
                studentTex.LoadImage(ImageToByteArray(_studentPath, sampleFileName));
                Color32[] stuImageDiffs = studentTex.GetPixels32();
                Color32[] stuImageColors = studentTex.GetPixels32();
                // Resample check, prompt student to redo resample if needed
                if (sampleTex.width != studentTex.width || sampleTex.height != studentTex.height) {
                    sceneText.text = sampleFileName + " (Check Console)";
                    sceneText.color = Color.red;
                    PASS = false;
                    Debug.Log("Your " + sampleFileName + " has different resolution than the samples we provided. Please change it back to " +
                              "the sample's resolution: " + sampleTex.width + "x" + sampleTex.height);
                }
                else {
                    // Image comparison
                    for (int i = 0; i < sampleImageColors.Length; i++) {
                        // Diff[diff.Count - 1] += ColorsDifferences(solImageColors[i], stuImageColors[i]);
                        if (ColorsDifferences(sampleImageColors[i], stuImageDiffs[i]) > 4) {
                            stuImageDiffs[i] = new Color32(255, 0, 0, 255);
                            diff[diff.Count - 1]++;
                        }   
                        else {
                            stuImageDiffs[i] = new Color32(0, 0, 0, 255);
                        }
                    }
                    // Add Colors array to its respective scene, one for sample, one for student
                    _sampleToColor.Add(sampleFileName, sampleImageColors);
                    _studentToColor.Add(sampleFileName, stuImageDiffs);
                    _studentToDiff.Add(sampleFileName, stuImageColors);
                    double score = Score(diff[diff.Count - 1], sampleTex.width, sampleTex.height);
                    sceneText.text = sampleFileName + " (" + score + "%)";
                    // Change colors according to score
                    if (score >= 95) {
                        sceneText.color = Color.green;
                    }
                    else {
                        sceneText.color = Color.red;
                        PASS = false;
                    }
                    yield return null;
                    Debug.Log(sampleFileName + ":" + sampleTex.width + "x" + sampleTex.height + ": " + Score(diff[diff.Count - 1], sampleTex.width, sampleTex.height) + "%");
                }
            }
        }
        TMP_Text resultText = GameObject.Find(_canvas + "Result").GetComponent<TMP_Text>();
        // Change colors according to score
        if (PASS) {
            resultText.text = "ALL PASSED!";
            resultText.color = Color.green;
        }
        else {
            resultText.text = "FAILED!";
            resultText.color = Color.red;
        }
    }

    public void SetTexture(String sampleFileName) {
        // Load the Color array back to Texture2D, then apply it to a RawImage GameObject
        _currentFileName = sampleFileName;
        Color32[] sampleImageColors = _sampleToColor[sampleFileName];
        Color32[] stuImageColors = _studentToColor[sampleFileName];
        Color32[] stuImageDiffs = _studentToDiff[sampleFileName];
        Texture2D sampleTex = new Texture2D(2, 2);
        sampleTex.LoadImage(ImageToByteArray(_samplePath, sampleFileName));
        sampleTex.SetPixels32(sampleImageColors);
        sampleTex.Apply();
        sampleImage.GetComponent<RawImage>().texture = sampleTex;
        Texture2D studentTex = new Texture2D(2, 2);
        studentTex.LoadImage(ImageToByteArray(_studentPath, sampleFileName));
        if (ISDIFFON) {
            studentTex.SetPixels32(stuImageColors);
        }
        else {
            studentTex.SetPixels32(stuImageDiffs);
        }
        studentTex.Apply();
        studentImage.GetComponent<RawImage>().texture = studentTex;
    }

    public void CheckboxDiff(bool diffEnabled) {
        ISDIFFON = diffEnabled;
        if (_currentFileName.Length == 0) {
            return;
        }
        Color32[] stuImageColors = _studentToColor[_currentFileName];
        Color32[] stuImageDiffs = _studentToDiff[_currentFileName];
        Texture2D studentTex = new Texture2D(2, 2);
        studentTex.LoadImage(ImageToByteArray(_studentPath, _currentFileName));
        if (ISDIFFON) {
            studentTex.SetPixels32(stuImageColors);
        }
        else {
            studentTex.SetPixels32(stuImageDiffs);
        }
        studentTex.Apply();
        studentImage.GetComponent<RawImage>().texture = studentTex;
    }

    private String FormatResult(string sceneText, double score) {
        // Result beautifier
        if (score.ToString().Length > 3) {
            sceneText = sceneText.Substring(0, sceneText.Length - ((score.ToString().Length - 4) * 2 + 1));
        }
        return sceneText + score + "%";
    }

    private double Score(double diff, int width, int height) {
        return Math.Round(100 - diff * 100.0f / (width * height), 1);
    }

    private double ColorsDifferences(Color32 x, Color32 y) {
        var red = Convert.ToSingle(x.r) - Convert.ToSingle(y.r);
        var green = Convert.ToSingle(x.g) - Convert.ToSingle(y.g);
        var blue = Convert.ToSingle(x.b) - Convert.ToSingle(y.b);
        return red * red + green * green + blue * blue;
    }
    
    private byte[] ImageToByteArray(String directoryPath, String sceneName) {
        var dirPath = Application.dataPath + directoryPath;
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);
        var imageBytes = File.ReadAllBytes(dirPath + sceneName + ".png");
        return imageBytes;
    }
}
