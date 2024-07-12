// Scene에서 UI를 사용하여 기능 동작할 수 있는 클래스입니다.
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class OpenAIRecommendSystemManager : MonoBehaviour
{
  public InputField inputField;
  public Button submitBtn;
  public Text cmd;
  public Text data;
  public List<TextAsset> textAssets;
  public List<Texture2D> itemImage = new List<Texture2D>();
  public int atlasSize = 512;

  public RawImage imageUI;
  public RawImage TextureimageUI;
  public RawImage integratedImageUI;

  private int currentIndex = 0;
  private string imagePath = "item image/IntegratedItemImage.png";
  private bool editing = false;
  
  
  void Start()
  {
      submitBtn.onClick.AddListener(OnSubmit);
  }

  public void OnSubmit()
  {
      OpenAIRecommendSystemLoader.Instance.SetItemTableParsing(textAssets);
      OpenAIRecommendSystemLoader.Instance.promptEditing(editing);
      OpenAIRecommendSystemLoader.Instance.GetItemRecommend(inputField.text, OnGenerateItem);
  }
  
  public void OnGenerateItem(bool res, string text)
  {
    submitBtn.interactable = true;
    if (res)
    {
     cmd.text = "진행상태 : 완료.";
     data.text = text;
     itemImage = OpenAIRecommendSystemLoader.Instance.LoadItemImage();
     if (itemImage.Count != 0) { imageUI.texture = itemImage[0]; }
     ItemImageIntegrated();
     }
     else { cmd.text = "실패"; }
  }
  
  public void OnGenerateImage(bool res, List<Texture> texture)
  {
     if (res)
     {
	     TextureimageUI.texture = texture[0];
     }
  }
  
 public void ItemImageIntegrated()
 {
     var image = itemImage.ToArray();
     Texture2D textureAtlas = new Texture2D(atlasSize, atlasSize);
     Rect[] rects = textureAtlas.PackTextures(image, 10, atlasSize);
     byte[] atlasBytes = textureAtlas.EncodeToPNG();

     if (Application.isEditor)
     {
         string path = Application.dataPath + "/Resources/" + imagePath;
         System.IO.File.WriteAllBytes(path, atlasBytes);
         if (System.IO.File.Exists(path))
         {
             byte[] fileData = System.IO.File.ReadAllBytes(path);
             var texture = new Texture2D(atlasSize, atlasSize);
             if (texture.LoadImage(fileData))
             {
                 integratedImageUI.texture = texture;
             }
         }
     }
     else
     {
         string directoryPath = Path.GetDirectoryName(imagePath);
         if (!Directory.Exists(imagePath))
         {
             Directory.CreateDirectory(directoryPath);
         }

         System.IO.File.WriteAllBytes(imagePath, atlasBytes);
         byte[] fileData = System.IO.File.ReadAllBytes(imagePath);
         if (textureAtlas.LoadImage(fileData))
         {
             integratedImageUI.texture = textureAtlas;
         }
     }
  }
}
