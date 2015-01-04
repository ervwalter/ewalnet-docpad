using GamesDataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using WebApi.OutputCache.V2;

namespace GamesWebAPI
{
    public class CollectionController : ApiController
    {
        [EnableCors("*","*","*")]
		[CacheOutput(ClientTimeSpan = 60)]
		public async Task<IEnumerable<CollectionItem>> Get()
        {
			var collection = await CacheManager.GetOrCreateObjectAsync("edwalter", true, 15, async (username) =>
			{
                var provider = new BggDataProvider();
				return await provider.GetCollection(username);
            });
            return collection.Games;
        }
    }

    public class PlaysController : ApiController
    {
		[CacheOutput(ClientTimeSpan = 60)]
		public async Task<IEnumerable<PlayItem>> Get()
        {
            var plays = await CacheManager.GetOrCreateObjectAsync("edwalter", true, 15, async (username) =>
            {
                var provider = new BggDataProvider();
				return await provider.GetPlays(username);
            });
            return plays.Items.Take(100);
        }

    }

	public class MenuExclusionsController : ApiController
	{
		[CacheOutput(ClientTimeSpan = 60)]
		public async Task<IEnumerable<string>> Get()
		{
			var geeklist = await CacheManager.GetOrCreateObjectAsync("183821", true, 15, async (id) =>
			{
				var provider = new BggDataProvider();
				return await provider.GetGeekList(id);
			});

			return geeklist.Items.Select(i => i.GameId);
		}

	}


}