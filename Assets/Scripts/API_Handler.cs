using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using UnityEngine.Networking;
using UnityEngine.UI;

public class API_Handler : MonoBehaviour
{
    [SerializeField]
    string apiUrl;
    [SerializeField]
    int tileSize;
    [SerializeField]
    GameObject loadingText;
    [SerializeField]
    MeshRenderer wallRenderer;

    [Space(10)]
    [SerializeField]
    List<APIObject> dataObject = new List<APIObject>();
    [SerializeField]
    List<Texture2D> textures = new List<Texture2D>();
    [SerializeField]
    API_States state = API_States.Init;


    public delegate void SyncDelegate();
    SyncDelegate syncDelegate;
    IAsyncResult asyncResult;


    private void Start()
    {
        state = API_States.Downloading;
        StartCoroutine(getData());
    }

    IEnumerator getData()
    {
        WWW www = new WWW(apiUrl);
        dataObject = new List<APIObject>();
        yield return www;

        if(www.error == null)
        {
            dataObject= JsonConvert.DeserializeObject<List<APIObject>>(www.text);
            state = API_States.Json;
        }
    }

    [SerializeField]
    int filesDownloaded=0;

    private void Update()
    {
        if(state == API_States.Json)
        {
            StartCoroutine(DownloadTexture(dataObject[filesDownloaded].Url));
        }
        if(state == API_States.Files)
        {
            loadingText.SetActive(false);

            CreateCheckerboard();
            wallRenderer.material.mainTexture = tiledTexture;
            state = API_States.Texture;
        }
    }

    Texture2D tiledTexture;

    void CreateCheckerboard()
    {
        tiledTexture = new Texture2D(1024, 1024);
        for (int y = 0; y < tiledTexture.height; y++)
        {
            for (int x = 0; x < tiledTexture.width; x++)
            {
                float t = EvaluateCheckerboardPixel(x, y);
                Color a = textures[0].GetPixel(x, y);
                Color b = textures[1].GetPixel(x, y);
                tiledTexture.SetPixel(x, y, Color.Lerp(a, b, t));
            }
        }
        tiledTexture.Apply();
    }

    float EvaluateCheckerboardPixel(int x, int y)
    {
        int width = tileSize;

        float valueX = (x % (width * 2.0f)) / (width * 2.0f);
        int vX = 1;
        if (valueX < 0.5f)
        {
            vX = 0;
        }

        float valueY = (y % (width * 2.0f)) / (width * 2.0f);
        int vY = 1;
        if (valueY < 0.5f)
        {
            vY = 0;
        }

        float value = 0;
        if (vX == vY)
        {
            value = 1;
        }
        return value;
    }

    IEnumerator DownloadTexture(string url)
    {
        state = API_States.Downloading;

        UnityWebRequest wr = new UnityWebRequest(url);
        DownloadHandlerTexture texDl = new DownloadHandlerTexture(true);
        wr.downloadHandler = texDl;
        yield return wr.SendWebRequest();
        if (!(wr.isNetworkError || wr.isHttpError))
        {
            Texture2D t = texDl.texture;
            textures.Add(t);
            filesDownloaded++;

            if(filesDownloaded < dataObject.Count)
            {
                state = API_States.Json;
            }
            else
            {
                state = API_States.Files;
            }
        }
    }
}

public enum API_States
{
    Init,
    Downloading,
    Json,
    Files,
    Texture
}

[JsonObject]
[System.Serializable]
public class APIObject
{
    [SerializeField]
    [JsonProperty("url")]
    string url;

    [SerializeField]
    [JsonProperty("width")]
    string width;

    [SerializeField]
    [JsonProperty("height")]
    string height;

    public string Url { get => url; set => url = value; }
    public string Width { get => width; set => width = value; }
    public string Height { get => height; set => height = value; }
}