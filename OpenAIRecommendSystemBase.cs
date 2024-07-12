// OpenAI를 이용해 필요한 메서드를 관리하는 베이스 클래스입니다.
using System;
using System.Collections.Generic;
using UnityEngine;

	public abstract class OpenAIRecommendSystemBase : MonoBehaviour
	{
	 static protected string key = "OpenAIKey";
 
	 public abstract void GetItemRecommend(string prompt, Action<bool, string> success);
	 public abstract void SetItemTableParsing(List<TextAsset> ta);
	 public abstract void GetItemImage(string prompt,Action<bool, List<Texture>> callback);
	 public abstract List<Texture2D> LoadItemImage();
	 public abstract void promptEditing(bool edit);
  }
