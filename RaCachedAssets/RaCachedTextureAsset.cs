using RaCaching.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RaCaching
{
	public class RaCachedTextureAsset : RaCachedAssetBase<Texture2D>
	{
		public const string AssetType = "RaCachedTextureAsset";

		[Obsolete("Deserialization Constructor")]
		public RaCachedTextureAsset()
			: base()
		{

		}

		public RaCachedTextureAsset(string url, Texture2D value, int lifetimeInDays)
			: base(AssetType, url, value, lifetimeInDays)
		{

		}

		public static bool Deserializer(string cachedType, string json, out IRaCachedAsset deserializedAsset)
		{
			return RaCacheUtils.TryDeserialize(AssetType, cachedType, json, out deserializedAsset);
		}

		protected override RaStatusResponse OnSave(Texture2D assetToSave)
		{
			RaStatusResponse response = new RaStatusResponse(false, string.Empty);

			try
			{
				string path = GetCachingPath();
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				File.WriteAllBytes(path, assetToSave.EncodeToPNG());

				response.Success = true;
				response.Message = string.Empty;
			}
			catch(Exception e)
			{
				response.Success = false;
				response.Message = e.Message;
			}

			return response;
		}

		protected override RaStatusValueTResponse OnLoad()
		{
			RaStatusValueTResponse response = new RaStatusValueTResponse(false, string.Empty, null);
			
			try
			{
				string path = GetCachingPath();
				if(File.Exists(path))
				{
					byte[] pngBytes = File.ReadAllBytes(path);
					Texture2D textureFromDisk = new Texture2D(1, 1);
					textureFromDisk.LoadImage(pngBytes);
					response.Success = true;
					response.Message = string.Empty;
					response.Value = textureFromDisk;
				}
				else
				{
					throw new Exception($"[{nameof(RaCachedTextureAsset.OnLoad)}]: File at '{path}' does not exist");
				}
			}
			catch(Exception e)
			{
				response.Success = false;
				response.Message = $"[{nameof(RaCachedTextureAsset.OnLoad)}]: {e.Message}";
				response.Value = default;
			}

			return response;
		}

		protected override RaStatusResponse OnRelease()
		{
			UnityEngine.Object.Destroy(LoadedValue);
			return RaStatusResponse.CreateSuccess();
		}

		protected override RaStatusResponse OnRemove()
		{
			RaStatusResponse response = new RaStatusResponse(false, string.Empty);
			
			try
			{
				string path = GetCachingPath();
				if(File.Exists(path))
				{
					File.Delete(path);
					response.Success = true;
					response.Message = string.Empty;
				}
				else
				{
					throw new Exception($"[{nameof(RaCachedTextureAsset.OnRemove)}]: File at '{path}' does not exist");
				}
			}
			catch(Exception e)
			{
				response.Success = false;
				response.Message = e.Message;
			}

			return response;
		}

		private string GetCachingPath()
		{
			return Path.Combine(Application.persistentDataPath, "RaCache", "Textures", Key + ".png");
		}
	}

	public static partial class RaCachedTextureAssetExtensions
	{
		public static RaStatusResponse SaveTexture(this RaCache raCache, string url, Texture2D texture, int lifetime = 5, bool serialize = true)
		{
			try
			{
				if(!raCache.HasCache(url))
				{
					return raCache.SetAsset(new RaCachedTextureAsset(url, texture, lifetime), serialize);
				}
				else
				{
					throw new Exception($"Url {url} is already cached");
				}
			}
			catch(Exception e)
			{
				return RaStatusResponse.CreateFailure($"[{nameof(RaCachedTextureAsset)}]: Failed: {e.Message}");
			}
		}

		public static RaCachedAssetBase<Texture2D>.RaStatusValueTResponse LoadTexture(this RaCache raCache, string url, bool refreshExpiration = true)
		{
			return raCache.LoadValue<Texture2D>(url, refreshExpiration);
		}
	}
}