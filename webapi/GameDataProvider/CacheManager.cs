using Lokad.Cloud.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GamesDataProvider
{
	public delegate Task<T> CreatorMethodAsync<T>(string key);

	public class CacheManager
	{
		private const string PartitionKey = "1";

		private static MemoryCache _cache = new MemoryCache("CustomCache");

		public static async Task<T> GetOrCreateObjectAsync<T>(string key, bool alwaysUseStorageCache, int memoryCacheDuration, CreatorMethodAsync<T> creator) where T : class
		{
			var result = await GetOrCreateObjectsAsync(new string[] { key }, alwaysUseStorageCache, memoryCacheDuration, creator);
			return result[key];

			//var type = typeof(T);
			//var cacheKey = GetCacheKey(key, type);
			//T result = null;
			//try
			//{
			//	result = _cache.Get(cacheKey) as T;
			//}
			//catch
			//{
			//	//absorb
			//}

			//// not in memory cache
			//if (result == null)
			//{
			//	if (alwaysUseStorageCache)
			//	{
			//		var table = GetTable<T>(type);
			//		var entity = table.Get(PartitionKey, key);
			//		if (entity != null && entity.HasValue)
			//		{
			//			result = entity.Value.Value;
			//		}

			//	}
			//	if (result == null)
			//	{
			//		result = await creator(key);
			//		await AddObjectAsync(key, memoryCacheDuration, result);
			//	}
			//}
			//return result;
		}

		public static async Task<Dictionary<string, T>> GetOrCreateObjectsAsync<T>(IEnumerable<string> keys, bool alwaysUseStorageCache, int memoryCacheDuration, CreatorMethodAsync<T> creator) where T : class
		{
			var todo = new List<string>(keys);
			var results = new Dictionary<string, T>();

			var type = typeof(T);
			foreach (var key in keys)
			{
				var cacheKey = GetCacheKey(key, type);
				T result = null;
				try
				{
					result = _cache.Get(cacheKey) as T;
				}
				catch
				{
					//absorb
				}
				if (result != null)
				{
					results.Add(key, result);
					todo.Remove(key);
				}
			}

			if (todo.Count > 0 && alwaysUseStorageCache)
			{
				var table = GetTable<T>();
				foreach (var entity in table.Get(PartitionKey, todo))
				{
					if (entity != null && entity.Value != null)
					{
						results.Add(entity.RowKey, entity.Value);
						CacheObject(entity.RowKey, memoryCacheDuration, entity.Value);
						todo.Remove(entity.RowKey);
					}
				}
			}

			if (todo.Count > 0)
			{
				foreach (var key in todo.ToArray())
				{
					try
					{
						var result = await creator(key);
						results.Add(key, result);
						todo.Remove(key);
						await AddObjectAsync(key, memoryCacheDuration, result);
					}
					catch
					{
						//absorb
					}

				}
			}

			if (todo.Count > 0)
			{
				var table = GetTable<T>();
				foreach (var entity in table.Get(PartitionKey, todo))
				{
					if (entity != null && entity.Value != null)
					{
						results.Add(entity.RowKey, entity.Value);
						CacheObject(entity.RowKey, memoryCacheDuration, entity.Value);
					}
				}
			}

			return results;
		}

		public static CloudTable<T> GetTable<T>() where T : class
		{
			return new CloudTable<T>(TableStorageProvider(), GetTypeName(typeof(T)));
		}

		public static async Task AddObjectAsync<T>(string key, int memoryCacheDuration, T value) where T : class
		{
			var type = typeof(T);
			CacheObject(key, memoryCacheDuration, value);
			var table = GetTable<T>();
			table.Upsert(new CloudEntity<T>
			{
				PartitionKey = PartitionKey,
				RowKey = key,
				Value = value
			});
		}

		private static void CacheObject<T>(string key, int memoryCacheDuration, T value) where T : class
		{
			var type = typeof(T);
			var cacheKey = GetCacheKey(key, type);
			_cache.Set(cacheKey, value, DateTimeOffset.Now.AddSeconds(memoryCacheDuration));
		}

		private static ITableStorageProvider TableStorageProvider()
		{
			return CloudStorage.ForAzureConnectionString(ConfigurationManager.ConnectionStrings["CacheManagerStorage"].ConnectionString).BuildTableStorage();
		}

		private static string GetCacheKey(string key, Type type)
		{
			return string.Format("{0}:{1}", GetTypeName(type), key);
		}

		private static string GetTypeName(Type type)
		{
			string friendlyName = type.Name;
			if (type.IsGenericType)
			{
				int iBacktick = friendlyName.IndexOf('`');
				if (iBacktick > 0)
				{
					friendlyName = friendlyName.Remove(iBacktick);
				}
				friendlyName += "Of";
				Type[] typeParameters = type.GetGenericArguments();
				for (int i = 0; i < typeParameters.Length; ++i)
				{
					string typeParamName = typeParameters[i].Name;
					friendlyName += (i == 0 ? typeParamName : "And" + typeParamName);
				}
			}

			return friendlyName;
		}
	}
}