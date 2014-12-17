using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace GamesWebAPI
{
    public class BggDataProvider
    {
        private BggClient _client = new BggClient();

        public async Task<Collection> GetCollection(string username)
        {
            var baseGames = _client.GetPartialCollection(username, false);
            var expansions = _client.GetPartialCollection(username, true);

            await Task.WhenAll(baseGames, expansions);

            //manually mark games as expansions if they are flagged as such in the comments
            foreach (var game in baseGames.Result)
            {
                if (game.UserComment != null && game.UserComment.Contains("%Expands:"))
                {
                    game.IsExpansion = true;
                }
            }

            return new Collection
            {
                Username = username,
                Games = baseGames.Result.Concat(expansions.Result).OrderBy(g => g.Name).ToList(),
                Timestamp = DateTimeOffset.Now,
            };
        }

        public async Task<Plays> GetPlays(string username)
        {
            var plays = await _client.GetPlays(username);
			var gameIds = new HashSet<string>(from play in plays select play.GameId);
			var games = await CacheManager.GetOrCreateObjectsAsync(gameIds, true, 600, (id) => _client.GetGame(id));
			foreach (var play in plays) {
				if (games.ContainsKey(play.GameId))
				{
					var game = games[play.GameId];
					play.Image = game.Image;
					play.Thumbnail = game.Thumbnail;
				}
			}

            return new Plays
            {
                Username = username,
                Items = plays,
                Timestamp = DateTimeOffset.Now
            };
        }
    }
}