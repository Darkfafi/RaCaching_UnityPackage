using System;
using System.Threading;
using System.Threading.Tasks;

namespace RaCaching
{
	public interface IRaCachedAsset
	{
		event Action<IRaCachedAsset> CachedAssetRemovedEvent;

		string CachingType
		{
			get;
		}

		string Key
		{
			get;
		}

		string Url
		{
			get;
		}

		DateTime RefreshedDateUTC
		{
			get;
		}

		DateTime ExpirationDateUTC
		{
			get;
		}

		bool IsExpired
		{
			get;
		}

		int LifetimeInDays
		{
			get;
		}

		void RefreshExpiration(int? overrideLifetimeInDays);

		/// <summary>
		/// Remove Asset from RAM
		/// </summary>
		RaStatusResponse Release();

		/// <summary>
		/// Remove Asset from Disk
		/// </summary>
		RaStatusResponse Remove();
	}
}