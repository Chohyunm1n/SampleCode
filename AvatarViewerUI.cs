//아바타 뷰어에서 아바타별 Thumbnail Texture를 캐싱하고, 일정 갯수 초과 시 Unload 해주는 코드입니다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class AvatarViewerUI : MonoBehaviour
{
    [Header("Avatar CustomizeBase")]
    public Customize targetAvatarForNew;
    private AvatarCore targetAvatarCoreForNew;
    private CustomizeTypeTable.CategoryTable[] categoryTables;
    
    private Dictionary<string, Dictionary<int, List<KeyValuePair<string, Texture2D>>>> categoryTextureCache = new Dictionary<string, Dictionary<int, List<KeyValuePair<string, Texture2D>>>>();
    private Queue<string> categoryKeyQueue = new Queue<string>();
    private Queue<int> itemKeyQueue = new Queue<int>();
    private int maxCache = 80;
    
    
    //아바타별 Thumbnail Texture 검사
     private Texture2D LoadThumbnailTexture(int index, int page)
    {
        var avatarType = targetAvatarForNew.TypeTable.typeKey;
        var thumbnailpath = Path.Combine(Application.persistentDataPath, "Thumbnails_" + avatarType);
        string itemKey = categoryTables[currCategoryIndex].itemList[index].itemKey;
        string categoryKey = categoryTables[currCategoryIndex].categoryKey;

        if (!categoryTextureCache.ContainsKey(categoryKey))
        {
            categoryTextureCache[categoryKey] = new Dictionary<int, List<KeyValuePair<string, Texture2D>>>();
        }
        
        var textureCache = categoryTextureCache[categoryKey];
        if (textureCache.ContainsKey(page))
        {
            var pageTextureList = textureCache[page];
            for (int i = 0; i < pageTextureList.Count; i++)
            {
                if (pageTextureList[i].Key.Equals(itemKey))
                {
                    return pageTextureList[i].Value;
                }
            }
            var texture = CreateThumbnail(thumbnailpath, itemKey);
            pageTextureList.Add(new KeyValuePair<string, Texture2D>(itemKey, texture));
            UnloadTexture(page, categoryKey);
	          return texture;
        }
        else
        {
            var texture = CreateThumbnail(thumbnailpath, itemKey);
            var newPageTextureList = new List<KeyValuePair<string, Texture2D>>();
            newPageTextureList.Add(new KeyValuePair<string, Texture2D>(itemKey, texture));
            textureCache.Add(page, newPageTextureList);
            UnloadTexture(page, categoryKey);
	          return texture;
        }
    }

    //캐싱된 Texture 갯수 검사
    private void UnloadTexture(int page, string categoryKey)
    {
        int pageCount = 0;
        int textureCount = 0;

        foreach (var data in categoryTextureCache)
        {
            foreach (var value in data.Value)
            {
                textureCount += value.Value.Count;
            }
            pageCount += data.Value.Count;
        }

        if (pageCount > maxCache || textureCount > (maxCache * 10))
        {
            var category = categoryKeyQueue.Peek();
            var item = itemKeyQueue.Peek();
            if (categoryTextureCache.TryGetValue(category, out var textureCache))
            {
                foreach (var data in textureCache)
                {
                   if(data.Key == item)
                    {
                        foreach(var value in data.Value)
                        {
                            Destroy(value.Value);
                            categoryKeyQueue.Dequeue();
                            itemKeyQueue.Dequeue();
                        }
                    }
                }
                categoryTextureCache[category].Remove(item);
            }
            categoryKeyQueue.Enqueue(categoryKey);
            itemKeyQueue.Enqueue(page);
        }
        else
        {
            categoryKeyQueue.Enqueue(categoryKey);
            itemKeyQueue.Enqueue(page);
        }
    }

//퍼시스턴스 경로에 있는 Texture 호출
private Texture2D CreateThumbnail(string thumbnailpath, string itemKey)
    {
        string path = Path.Combine(thumbnailpath, itemKey) + ".png";

        if (File.Exists(path))
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(0, 0);
            texture.LoadImage(bytes);
	            return texture;
        }
        return null;
    }

    //캐싱된 카테고리별 텍스쳐 Unload
    private void UnloadAllCachedData()
    {
        if (categoryTextureCache.Count != 0)
        {
            var categoryKeys = new List<string>(categoryTextureCache.Keys);
            foreach (var categoryKey in categoryKeys)
            {
                UnloadCategoryCachedData(categoryKey);
            }
        }
    }

    private void UnloadCategoryCachedData(string categoryKey)
    {
        if (categoryTextureCache.TryGetValue(categoryKey, out var textureCache))
        {
            foreach (var texture in textureCache)
            {
                var pageTextureList = texture.Value;

                foreach (var pageTexture in pageTextureList)
                {
                    Destroy(pageTexture.Value);

                }
                pageTextureList.Clear();
            }
            categoryTextureCache.Remove(categoryKey);
        }
        categoryKeyQueue.Clear();
        itemKeyQueue.Clear();

        Resources.UnloadUnusedAssets();
    }  
    
}
