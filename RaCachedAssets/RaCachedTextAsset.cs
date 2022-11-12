using RaCaching.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RaCaching
{
	public class RaCachedTextAsset : RaCachedAssetBase<string>
	{
		public const string AssetType = "RaCachedTextureAsset";

		[Obsolete("Deserialization Constructor")]
		public RaCachedTextAsset()
			: base()
		{

		}

		public RaCachedTextAsset(string url, string text, int lifetimeInDays)
			: base(AssetType, url, text, lifetimeInDays)
		{

		}

		public static bool Deserializer(string cachedType, string json, out IRaCachedAsset deserializedAsset)
		{
			return RaCacheUtils.TryDeserialize(AssetType, cachedType, json, out deserializedAsset);
		}

		protected override RaStatusResponse OnSave(string assetToSave)
		{
			PlayerPrefs.SetString(Key, assetToSave);
			return RaStatusResponse.CreateSuccess();
		}

		protected override RaStatusValueTResponse OnLoad()
		{
			if(PlayerPrefs.HasKey(Key))
			{
				string value = PlayerPrefs.GetString(Key);
				return RaStatusValueTResponse.CreateSuccess(value);
			}
			else
			{
				return RaStatusValueTResponse.CreateFailure($"Key '{Key}' not found!");
			}
		}

		protected override RaStatusResponse OnRelease()
		{
			return RaStatusResponse.CreateSuccess();
		}

		protected override RaStatusResponse OnRemove()
		{
			PlayerPrefs.DeleteKey(Key);
			return RaStatusResponse.CreateSuccess();
		}
	}

	public static partial class RaCachedTextureAssetExtensions
	{
		public static RaStatusResponse SaveText(this RaCache raCache, string url, string text, int lifetime = 5, bool serialize = true)
		{
			try
			{
				if(!raCache.HasCache(url))
				{
					return raCache.SetAsset(new RaCachedTextAsset(url, text, lifetime), serialize);
				}
				else
				{
					throw new Exception($"Url {url} is already cached");
				}
			}
			catch(Exception e)
			{
				return RaStatusResponse.CreateFailure($"[{nameof(RaCachedTextAsset)}]: Failed: {e.Message}");
			}
		}

		public static RaCachedAssetBase<string>.RaStatusValueTResponse LoadText(this RaCache raCache, string url, bool refreshExpiration = true)
		{
			return raCache.LoadValue<string>(url, refreshExpiration);
		}
	}
}