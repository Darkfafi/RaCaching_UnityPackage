using RaCaching.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RaCaching
{
	public abstract class RaCachedAssetBase<ValueT> : IRaCachedAsset
	{
		public event Action<IRaCachedAsset> CachedAssetRemovedEvent;

		#region Serialization

		[SerializeField]
		private string _cachingType;

		[SerializeField]
		private string _url;

		[SerializeField]
		private string _key;

		[SerializeField]
		private long _refreshedDateTimeUTC_Binary;

		[SerializeField]
		private long _expirationDateTimeUTC_Binary;

		[SerializeField]
		private int _lifetimeInDays;

		#endregion

		#region Variables

		private DateTime? _refreshedDateUTC = null;
		private DateTime? _expirationDateUTC = null;

		#endregion

		#region Properties

		public string CachingType => _cachingType;
		public string Url => _url;
		public string Key => _key;
		public int LifetimeInDays => _lifetimeInDays;

		public bool IsExpired => DateTime.UtcNow >= ExpirationDateUTC;

		public DateTime RefreshedDateUTC
		{
			get
			{
				if(!_refreshedDateUTC.HasValue)
				{
					_refreshedDateUTC = DateTime.FromBinary(_refreshedDateTimeUTC_Binary);
				}

				return _refreshedDateUTC.Value;
			}
			private set
			{
				_refreshedDateUTC = value;
				_refreshedDateTimeUTC_Binary = value.ToBinary();
			}
		}

		public DateTime ExpirationDateUTC
		{
			get
			{
				if(!_expirationDateUTC.HasValue)
				{
					_expirationDateUTC = DateTime.FromBinary(_expirationDateTimeUTC_Binary);
				}

				return _expirationDateUTC.Value;
			}
			private set
			{
				_expirationDateUTC = value;
				_expirationDateTimeUTC_Binary = value.ToBinary();
			}
		}

		protected ValueT LoadedValue
		{
			get; private set;
		}

		#endregion

		/// <summary>
		/// Deserialization Constructor
		/// </summary>
		[Obsolete("This is intended for Deserialization Only")]
		public RaCachedAssetBase()
		{

		}

		/// <summary>
		/// New Instance Constructor
		/// </summary>
		public RaCachedAssetBase(string cachingType, string url, ValueT value, int lifetimeInDays)
		{
			_cachingType = cachingType;
			_url = url;
			_key = RaCache.GenerateKey(url);
			RefreshExpiration(lifetimeInDays);
			var saveResult = Save(value);
			if(!saveResult.Success)
			{
				throw new Exception($"Initial Save Failed - Error: {saveResult.Message}");
			}
		}

		public RaStatusValueTResponse Load()
		{
			if(LoadedValue != null)
			{
				return new RaStatusValueTResponse(true, "Loaded Value", LoadedValue);
			}
			else
			{
				var response = OnLoad();
				if(response.Success)
				{
					LoadedValue = response.Value;
				}
				return response;
			}
		}

		public RaStatusResponse Save(ValueT assetToSave)
		{
			var response = OnSave(assetToSave);
			if(response.Success)
			{
				LoadedValue = assetToSave;
			}
			return response;
		}

		public RaStatusResponse Release()
		{
			var response = OnRelease();
			if(response.Success)
			{
				LoadedValue = default;
			}
			return response;
		}

		public RaStatusResponse Remove()
		{
			var response = OnRemove();
			if(response.Success)
			{
				LoadedValue = default;
				CachedAssetRemovedEvent?.Invoke(this);
			}
			return response;
		}

		public void RefreshExpiration(int? overrideLifetimeInDays = null)
		{
			RefreshedDateUTC = DateTime.UtcNow;

			if(overrideLifetimeInDays.HasValue)
			{
				_lifetimeInDays = overrideLifetimeInDays.Value;
			}

			if(_lifetimeInDays >= 0)
			{
				ExpirationDateUTC = RefreshedDateUTC.AddDays(_lifetimeInDays);
			}
			else
			{
				ExpirationDateUTC = DateTime.MaxValue;
			}
		}

		protected abstract RaStatusValueTResponse OnLoad();
		protected abstract RaStatusResponse OnSave(ValueT assetToSave);
		protected abstract RaStatusResponse OnRelease();
		protected abstract RaStatusResponse OnRemove();

		#region Nested

		[Serializable]
		public struct RaStatusValueTResponse
		{
			public bool Success;
			public string Message;
			public ValueT Value;

			public RaStatusValueTResponse(bool success, string message, ValueT value)
			{
				Success = success;
				Message = message;
				Value = value;
			}

			public static RaStatusValueTResponse CreateFailure(string message)
			{
				return new RaStatusValueTResponse(false, message, default);
			}

			public static RaStatusValueTResponse CreateSuccess(ValueT value, string message = "")
			{
				return new RaStatusValueTResponse(true, message, value);
			}
		}

		#endregion
	}
}