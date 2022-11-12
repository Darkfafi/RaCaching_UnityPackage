using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RaCaching.Utils
{
	public static class RaCacheUtils
	{
		public async static Task WaitUntil(Func<bool> predicate, CancellationToken cancellationToken)
		{
			while(!predicate())
			{
				await Task.Delay(100, cancellationToken);
			}
		}

		public static Sprite CreateSprite(this Texture2D texture)
		{
			return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f);
		}

		public static bool TryDeserialize(this string myCacheType, string cacheType, string json, out IRaCachedAsset deserializedAsset)
		{
			if(myCacheType == cacheType)
			{
				try
				{
					deserializedAsset = JsonUtility.FromJson<RaCachedTextureAsset>(json);
					return true;
				}
				catch(Exception e)
				{
					Debug.LogError($"[{nameof(RaCacheUtils.TryDeserialize)}]: {e.Message}");
					deserializedAsset = default;
					return false;
				}
			}
			deserializedAsset = default;
			return false;
		}
	}
}
