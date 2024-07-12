// 싱글톤 패턴을 사용했습니다.
// OpenAI를 사용하기 위해 알맞는 데이터 형식으로 가공하고, 키워드를 추출하여 이미지들을 호출하는 코드입니다.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;

public class OpenAIRecommendSystemLoader : OpenAIRecommendSystemBase
{
    static string OpenAIURL = "https://api.openai.com/v1/chat/completions";
    static string objectName = "AvatarItemRecommendSystem";

    protected static OpenAIRecommendSystemBase instance;
    public static OpenAIRecommendSystemBase Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject gameObject = new GameObject(objectName);
                DontDestroyOnLoad(gameObject);
                instance = gameObject.AddComponent<OpenAIRecommendSystemLoader>();
            }
            return instance;
        }
    }

    [System.Serializable]
    public class RequestData
    {
        public string model;
        public Messages[] messages;
        public float temperature;
    }

    [System.Serializable]
    public class Messages
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class OpenAIChatCompletion
    {
        public string id;
        public string model;
        public List<ChatChoice> choices;

        public Usage usage;
    }
    [System.Serializable]
    public class Usage
    {
        public int completion_tokens;
        public int prompt_tokens;
        public int total_tokens;
    }

    [System.Serializable]
    public class ChatChoice
    {
        public Messages message;
    }

    private List<Texture2D> loaditemImage = new List<Texture2D>();
    private List<Dictionary<string, object>> CategoryItemData = new List<Dictionary<string, object>>();
    private Dictionary<string, string> itemRecommendData = new Dictionary<string, string>();
    private string conversationHistory = "";
    private bool editState = false;
    private StringBuilder previousData = new StringBuilder();
    private string user = "user";
    private string jsonData;
    private string ChatGPTConditionData = "";
    private string ChatGPTConditionData2 = "";
    private string ChatGPTConditionData3 = "";
    private Stopwatch stopwatch = new Stopwatch();
    
    public void ParsingCSV(List<TextAsset> ta)
    {
        if (jsonData == null)
        {
            if (ta != null)
            {
                for (int j = 0; j < ta.Count; j++)
                {
                    List<Dictionary<string, object>> taData = CSVReader.Read(ta[j]);
                    CategoryItemData.AddRange(taData);
                }
                string json = JsonConvert.SerializeObject(CategoryItemData, Formatting.Indented);
                jsonData = json;
                File.WriteAllText(Application.dataPath + "/" + "ItemDataJson.json", json);
            }
        }
    }

    //OpenAI API ChatGPT를 통해 텍스트 출력
    public override void GetItemRecommend(string prompt, Action<bool, string> success)
    {
        var data = CreateJsonPromptForItem(prompt, user, jsonData, conversationHistory);
        UnityEngine.Debug.Log(data);
        var request = new UnityEngine.Networking.UnityWebRequest(OpenAIURL, "POST");
        request.SetRequestHeader("Authorization", key);
        request.SetRequestHeader("Content-Type", "application/json");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(data);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();


        StartCoroutine(SendRequestCoroutine(request, success));
    }
    
    IEnumerator SendRequestCoroutine(UnityWebRequest request, Action<bool, string> success)
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {

            var responseData = JsonUtility.FromJson<OpenAIChatCompletion>(request.downloadHandler.text);
            UnityEngine.Debug.Log(responseData.choices[0].message.content);
            previousData.Append(responseData.choices[0].message.role);
            previousData.Append(responseData.choices[0].message.content);
            conversationHistory = responseData.choices[0].message.content;
            UnityEngine.Debug.Log("총 토큰 사용량 : " + responseData.usage.total_tokens);
            var itemRecommendList = ParsingContentData(responseData.choices[0].message.content);
            itemRecommendData = itemRecommendList;
            success?.Invoke(true, ItemTextUI());
        }
        else
        {
            UnityEngine.Debug.LogError("Error: " + request.error);
        }
    }
    
    private string CreateJsonPromptForItem(string prompt, string user, string jsonData, string conversationHistory)
    {
        if (editState)
        {
            string content = $"itemList : {jsonData}, find the item that matches the user input:{ChatGPTConditionData + ChatGPTConditionData2}.";
            if (!string.IsNullOrEmpty(conversationHistory))
            {
                content = $"{conversationHistory}\n{user}: {prompt}\n{content}";
            }

            RequestData data = new RequestData
            {
                model = "gpt-4-1106-preview",
                temperature = 1f,
                messages = new Messages[]
            {
                    new Messages { role = "assistant", content = content },
                    new Messages { role = user, content = $"condition : {ChatGPTConditionData3} + \n +편집 지시문 : {prompt}" } }
            };

            previousData.Append(data.messages[1].role);
            previousData.Append(data.messages[1].content);
            //response 시간 확인, 이전 데이터 캐싱
            return JsonUtility.ToJson(data);
        }
        else
        {
            previousData.Clear();
            RequestData data = new RequestData
            {
                model = "gpt-4-1106-preview",
                temperature = 1f,
                messages = new Messages[]
            {
                    new Messages { role = "assistant", content = $"condition : {ChatGPTConditionData + "\n" + ChatGPTConditionData2}" },
                    new Messages { role = user, content = $"itemList : {jsonData}, user input:{prompt}" } }
            };

            for (int i = 0; i < data.messages.Length; i++)
            {
                previousData.Append(data.messages[i].role);
                previousData.Append(data.messages[i].content);
            }
            return JsonUtility.ToJson(data);
        }
    }

    public override void SetItemTableParsing(List<TextAsset> ta)
    {
        ParsingCSV(ta);
    }

    public Dictionary<string, string> ParsingContentData(string data)
    {
        var customization = new Dictionary<string, string>();
        string pattern = @"(.*?): \[(.*?)\]";

        foreach (Match match in Regex.Matches(data, pattern))
        {
            if (match.Groups.Count == 3)
            {
                string category = match.Groups[1].Value.Trim();
                string item = match.Groups[2].Value.Trim();

                if (!string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(item))
                {
                    customization[category] = item;
                }
            }
        }
        return customization;
    }

    public string ItemTextUI()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var t in itemRecommendData)
        {
            sb.Append(t.Key + " : " + t.Value);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    // 추출된 아이템 데이터를 가지고 아이템 텍스쳐 호출
    public override List<Texture2D> LoadItemImage()
    {
        if (loaditemImage.Count != 0) loaditemImage.Clear();
        if (itemRecommendData != null)
        {
            string resourcePath;
            foreach (var itemKey in itemRecommendData)
            {
                if (Application.isEditor)
                {
                    resourcePath = Application.dataPath + "/Resources/item image/" + itemKey.Key;
                    if (Directory.Exists(resourcePath))
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(resourcePath);

                        foreach (FileInfo file in directoryInfo.GetFiles())
                        {
                            if (file.Extension == ".meta") continue;

                            if (file.Name.Contains(itemKey.Value))
                            {
                                var bytes = File.ReadAllBytes(file.FullName);
                                var texture = new Texture2D(0, 0);
                                texture.LoadImage(bytes);
                                loaditemImage.Add(texture);
                            }
                        }
                    }
                }
                else
                {
                    resourcePath = "item image/" + itemKey.Key;
                    var texture = Resources.LoadAll<Texture2D>(resourcePath);
                    foreach (var tex in texture)
                    {
                        if (tex.name.Contains(itemKey.Value))
                        {
                            loaditemImage.Add(tex);
                        }
                    }
                }
            }
            return loaditemImage;
        }
        return null;
    }

    private void RequestTime()
    {
        Stopwatch stopwatch = new Stopwatch();

        if (stopwatch.IsRunning)
        {
            UnityEngine.Debug.Log("Request 시간 : " + stopwatch.Elapsed);
            stopwatch.Stop();
            stopwatch.Reset();
        }
        else
        {
            stopwatch.Start();
        }
    }
}
