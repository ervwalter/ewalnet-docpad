using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace GamesDataProvider
{
	public class BggDataProvider
	{
		private BggClient _client = new BggClient();

		public async Task<Collection> GetCollection(string username)
		{
			var partialList1 = _client.GetPartialCollection(username, false);
			var partialList2 = _client.GetPartialCollection(username, true);
			await Task.WhenAll(partialList1, partialList2);

			var games = partialList1.Result.Concat(partialList2.Result).OrderBy(g => g.Name);
			var gamesById = games.ToLookup(g => g.GameId);

			// manually mark games as expansions if they are flagged as such in the comments
			foreach (var game in games)
			{
				if (!string.IsNullOrWhiteSpace(game.UserComment) && game.UserComment.Contains("%Expands:"))
				{
					game.IsExpansion = true;
				}
			}

			var expansions = from g in games
							 where g.IsExpansion
							 orderby g.Name
							 select g;

			var gameIds = new HashSet<string>(games.Select(g => g.GameId));
			var gameDetailsById = await CacheManager.GetOrCreateObjectsAsync(gameIds, true, 600, async (id) => await GetGame(id));

			foreach (var game in games)
			{
				if (gameDetailsById.ContainsKey(game.GameId))
				{
					var gameDetails = gameDetailsById[game.GameId];
					game.Mechanics = gameDetails.Mechanics;
					game.BGGRating = gameDetails.BggRating;
					game.AverageWeight = gameDetails.AverageWeight;
					game.Artists = gameDetails.Artists;
					game.Publishers = gameDetails.Publishers;
					game.Designers = gameDetails.Designers;
				}
			}

			Regex expansionCommentExpression = new Regex(@"%Expands:(.*\w+.*)\[(\d+)\]", RegexOptions.Compiled);
			foreach (var expansion in expansions)
			{
				if (gameDetailsById.ContainsKey(expansion.GameId))
				{
					var expansionDetails = gameDetailsById[expansion.GameId];
					if (expansionDetails != null)
					{
						var expandsLinks = new List<BoardGameLink>(expansionDetails.Expands ?? new List<BoardGameLink>());
						if (!string.IsNullOrWhiteSpace(expansion.UserComment) && expansion.UserComment.Contains("%Expands:"))
						{
							var match = expansionCommentExpression.Match(expansion.UserComment);
							if (match.Success)
							{
								var name = match.Groups[1].Value.Trim();
								var id = match.Groups[2].Value.Trim();
								expandsLinks.Add(new BoardGameLink
								{
									GameId = id,
									Name = name
								});
								expansion.UserComment = expansionCommentExpression.Replace(expansion.UserComment, "").Trim();
							}
						}
						foreach (var link in expandsLinks)
						{
							var parentGames = gamesById[link.GameId];
							foreach (var game in parentGames)
							{
								if (game.IsExpansion)
								{
									continue;
								}
								if (game.Expansions == null)
								{
									game.Expansions = new List<CollectionItem>();
								}
								game.Expansions.Add(expansion.Clone());
							}
						}
					}
				}
			}

			Regex removeArticles = new Regex(@"^the\ |a\ |an\ ");

			games = from g in games
					where !g.IsExpansion
					orderby removeArticles.Replace(g.Name.ToLower(), "")
					select g;

			var plays = await CacheManager.GetOrCreateObjectAsync(username, true, 15, async (u) => await this.GetPlays(u));
			var playsByGame = plays.Items.ToLookup(p => p.GameId);

			foreach (var game in games)
			{
				if (playsByGame.Contains(game.GameId))
				{
					game.PlayDates = (from play in playsByGame[game.GameId]
									  where play.PlayDate.HasValue
									  select play.PlayDate.Value).ToList();
				}
				else
				{
					game.PlayDates = new List<DateTime>();
				}
			}

			return new Collection
			{
				Username = username,
				Games = games.ToList(),
				Timestamp = DateTimeOffset.UtcNow,
			};
		}

		public async Task<Plays> GetPlays(string username)
		{
			var plays = await _client.GetPlays(username);
			var gameIds = new HashSet<string>(from play in plays select play.GameId);
			var games = await CacheManager.GetOrCreateObjectsAsync(gameIds, true, 600, async (id) => await GetGame(id));
			foreach (var play in plays)
			{
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
				Timestamp = DateTimeOffset.UtcNow
			};
		}

		public async Task<GameDetails> GetGame(string gameId)
		{
			var game = await _client.GetGame(gameId);
			game.Timestamp = DateTimeOffset.UtcNow;
			return game;
		}

		public async Task<GeekList> GetGeekList(string id)
		{
			var geeklist = await _client.GetGeekList(id);
			geeklist.Timestamp = DateTimeOffset.UtcNow;
			return geeklist;
		}
	}
}