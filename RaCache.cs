using System;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RaCaching
{
	public class RaCache
	{
		public const string RaCacheStorage_CountKey = "_RaCaching_RaCache_CachedAssets_Count_";
		public const string RaCacheStorage_Asset_X_Key = "_RaCaching_RaCache_CachedAssets_Asset_{0}";

		public delegate bool RaCachedAssetDeserializeHandler(string cachingType, string json, out IRaCachedAsset deserializedAsset);
		private readonly List<RaCachedAssetDeserializeHandler> _deserializers;
		private readonly List<IRaCachedAsset> _cachedAssets = new List<IRaCachedAsset>();
		private readonly Dictionary<string, IRaCachedAsset> _cachedAssetsMap = new Dictionary<string, IRaCachedAsset>();

		public RaCache(params RaCachedAssetDeserializeHandler[] deserializers)
		{
			// Built-in Deserializers
			_deserializers = new List<RaCachedAssetDeserializeHandler>()
			{
				RaCachedTextAsset.Deserializer,
				RaCachedTextureAsset.Deserializer
			};

			if(deserializers != null)
			{
				_deserializers.AddRange(deserializers);
			}

			Deserialize();
			RemoveExpiredAssets();
		}

		public static string GenerateKey(string url)
		{
			byte[] data = Encoding.ASCII.GetBytes(url);
			using SHA1 sha = SHA1.Create();
			data = sha.ComputeHash(data);
			string hash = BitConverter.ToString(data).Replace("-", "");
			return string.Concat(hash.Substring(0, Mathf.Min(hash.Length, 10)), "-", hash.Substring(Mathf.Max(0, hash.Length - 10), Mathf.Min(hash.Length, 10)));
		}

		public RaCachedAssetBase<TValue>.RaStatusValueTResponse LoadValue<TValue>(string url, bool refreshExpiration = true)
		{
			RaCachedAssetBase<TValue>.RaStatusValueTResponse response = RaCachedAssetBase<TValue>.RaStatusValueTResponse.CreateFailure(string.Empty);

			if(TryGetAsset(url, out RaCachedAssetBase<TValue> asset))
			{
				if(refreshExpiration)
				{
					asset.RefreshExpiration();
				}
				response = asset.Load();
			}
			else
			{
				response.Success = false;
				response.Value = default;
				response.Message = $"Asset under url '{url}' not found";
			}

			return response;
		}

		public bool HasCache(string url)
		{
			string key = GenerateKey(url);
			return _cachedAssetsMap.ContainsKey(key);
		}

		public RaStatusResponse SetAsset<T>(T asset, bool serialize = true)
			where T : IRaCachedAsset
		{
			if(!_cachedAssetsMap.ContainsKey(asset.Key))
			{
				_cachedAssets.Add(asset);
				_cachedAssetsMap[asset.Key] = asset;
				asset.CachedAssetRemovedEvent += OnCachedAssetRemovedEvent;

				if(serialize)
				{
					Serialize();
				}

				return RaStatusResponse.CreateSuccess();
			}
			return RaStatusResponse.CreateFailure($"An asset with key {asset.Key} already exists");
		}

		public void RemoveExpiredAssets()
		{
			RemoveAllAssets(x => x.IsExpired);
		}

		public void RemoveAllAssets(Predicate<IRaCachedAsset> predicate = null)
		{
			for(int i = _cachedAssets.Count - 1; i >= 0; i--)
			{
				IRaCachedAsset assetToRemove = _cachedAssets[i];
				if(predicate == null || predicate(assetToRemove))
				{
					var response = RemoveAsset(assetToRemove.Url);
					if(!response.Success)
					{
						Debug.LogError(i + ": " + response.Message);
					}
				}
			}
		}

		public bool TryGetAsset<T>(string url, out T asset)
			where T : IRaCachedAsset
		{
			string key = GenerateKey(url);
			if(_cachedAssetsMap.TryGetValue(key, out IRaCachedAsset rawValue) && rawValue is T castedValue)
			{
				asset = castedValue;
				return true;
			}
			asset = default;
			return false;
		}

		public RaStatusResponse RemoveAsset(string url)
		{
			string key = GenerateKey(url);
			RaStatusResponse response = new RaStatusResponse(true, string.Empty);
			if(_cachedAssetsMap.TryGetValue(key, out IRaCachedAsset rawValue))
			{
				RaStatusResponse[] responses = new RaStatusResponse[]
				{
					rawValue.Release(),
					rawValue.Remove()
				};

				for(int i = 0; i < responses.Length; i++)
				{
					response = responses[i];
					if(!response.Success)
					{
						break;
					}
				}
			}
			else
			{
				response.Success = false;
				response.Message = $"Asset under key '{key}' not found";
			}

			return response;
		}

		public void Serialize()
		{
			// Delete Old Storage
			for(int i = 0, c = PlayerPrefs.GetInt(RaCacheStorage_CountKey, 0); i < c; i++)
			{
				string assetKey = GetAssetPrefKey(i);
				PlayerPrefs.DeleteKey(assetKey);
			}
			PlayerPrefs.DeleteKey(RaCacheStorage_CountKey);

			// Write New Storage
			int countSerialized = 0;
			for(int i = 0, c = _cachedAssets.Count; i < c; i++)
			{
				string assetKey = GetAssetPrefKey(countSerialized);
				try
				{
					CachedAssetStorageEntry storageEntry = new CachedAssetStorageEntry(_cachedAssets[i]);
					string assetJSON = JsonUtility.ToJson(storageEntry);
					PlayerPrefs.SetString(assetKey, assetJSON);
					countSerialized++;
				}
				catch(Exception e)
				{
					Debug.LogError($"[{nameof(RaCache.Serialize)}]: Asset {assetKey} failed to Serialize. Error {e.Message}");
				}
			}
			PlayerPrefs.SetInt(RaCacheStorage_CountKey, countSerialized);
			PlayerPrefs.Save();
		}

		public void Deserialize()
		{
			_cachedAssets.Clear();
			if(PlayerPrefs.HasKey(RaCacheStorage_CountKey))
			{
				int countSerialized = PlayerPrefs.GetInt(RaCacheStorage_CountKey);
				for(int i = 0; i < countSerialized; i++)
				{
					string assetKey = GetAssetPrefKey(i);
					if(PlayerPrefs.HasKey(assetKey))
					{
						try
						{
							CachedAssetStorageEntry storageEntry = JsonUtility.FromJson<CachedAssetStorageEntry>(PlayerPrefs.GetString(assetKey));
							if(TryDeserializeCachedAsset(storageEntry, out IRaCachedAsset deserializedAsset))
							{
								SetAsset(deserializedAsset, false);
							}
							else
							{
								throw new Exception("No Deserializer found to Deserialize Asset");
							}
						}
						catch(Exception e)
						{
							Debug.LogError($"[{nameof(RaCache.Deserialize)}]: Asset {assetKey} failed to Deserialize. Error {e.Message}");
						}
					}
				}
			}
		}

		private bool TryDeserializeCachedAsset(CachedAssetStorageEntry storageEntry, out IRaCachedAsset deserializedAsset)
		{
			if(storageEntry.IsValid)
			{
				for(int i = 0; i < _deserializers.Count; i++)
				{
					RaCachedAssetDeserializeHandler deserializer = _deserializers[i];
					if(deserializer.Invoke(storageEntry.CachingType, storageEntry.CachedAssetJSON, out deserializedAsset))
					{
						return true;
					}
				}
			}

			deserializedAsset = default;
			return false;
		}

		private static string GetAssetPrefKey(int index)
		{
			return string.Format(RaCacheStorage_Asset_X_Key, index.ToString(CultureInfo.InvariantCulture));
		}

		private void OnCachedAssetRemovedEvent(IRaCachedAsset removedAsset)
		{
			removedAsset.CachedAssetRemovedEvent -= OnCachedAssetRemovedEvent;
			_cachedAssetsMap.Remove(removedAsset.Key);
			_cachedAssets.Remove(removedAsset);
			Serialize();
		}

		[Serializable]
		public struct CachedAssetStorageEntry
		{
			public string CachingType;
			public string CachedAssetJSON;

			public bool IsValid => !string.IsNullOrEmpty(CachingType) && !string.IsNullOrEmpty(CachedAssetJSON);

			public CachedAssetStorageEntry(IRaCachedAsset asset)
			{
				CachingType = asset.CachingType;
				CachedAssetJSON = JsonUtility.ToJson(asset);
			}
		}
	}
}