using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace GamesWebAPI
{
    public class CollectionController : ApiController
    {
        // GET api/<controller>
        public async Task<List<CollectionItem>> Get()
        {
            var collection = await CacheManager.GetOrCreateObjectAsync("edwalter", false, 15, async (k) => {
                var provider = new BggDataProvider();
                return await provider.GetCollection("edwalter");
            });
            return collection.Games;
        }
    }

    public class PlaysController : ApiController
    {
        // GET api/<controller>
        public async Task<List<PlayItem>> Get()
        {
            var plays = await CacheManager.GetOrCreateObjectAsync("edwalter", false, 15, async (k) =>
            {
                var provider = new BggDataProvider();
                return await provider.GetPlays("edwalter");
            });
            return plays.Items;
        }

    }

}