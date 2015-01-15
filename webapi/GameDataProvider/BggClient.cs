using Flurl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace GamesDataProvider
{
	public class BggClient
	{
		private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);
		private static DateTimeOffset _lastDownloadCompleted = DateTimeOffset.MinValue;
		private static readonly TimeSpan MinimumTimeBetweenDownloads = new TimeSpan(0, 0, 0, 1, 100); // 1.1 second between BGG requests to prevent them from blocking us

		private const string BaseUrl = "http://www.boardgamegeek.com/xmlapi2/";
		private const string LegacyBaseUrl = "http://www.boardgamegeek.com/xmlapi/";


		public async Task<List<CollectionItem>> GetPartialCollection(string username, bool expansions)
		{
			var url = new Url(BaseUrl).AppendPathSegment("/collection").SetQueryParams(new
			{
				username = username,
				stats = 1
			});

			if (expansions)
			{
				url.SetQueryParam("subtype", "boardgameexpansion");
			}
			else
			{
				url.SetQueryParam("excludesubtype", "boardgameexpansion");

			}

			var data = await DownloadData(url.ToString());
			var items = from item in data.Descendants("item")
						select new CollectionItem
						{
							GameId = item.AttributeAs<string>("objectid"),
							Name = item.Element("name").As<string>(),
							Image = item.Element("image").As<string>(),
							Thumbnail = item.Element("thumbnail").As<string>(),

							IsExpansion = expansions,
							YearPublished = item.Element("yearpublished").As<int>(),

							MinPlayers = item.Element("stats").AttributeAs<int>("minplayers"),
							MaxPlayers = item.Element("stats").AttributeAs<int>("maxplayers"),
							PlayingTime = item.Element("stats").AttributeAs<int>("playingtime"),

							AverageRating = item.Element("stats").Element("rating").Element("average").AttributeAs<decimal?>("value"),
							Rank = ParseRanking(item.Element("stats").Element("rating")),

							NumPlays = item.Element("numplays").As<int>(),
							Rating = item.Element("stats").Element("rating").AttributeAs<decimal?>("value", null, "n/a"),

							Owned = item.Element("status").AttributeAs<int>("own").AsBool(),
							PreOrdered = item.Element("status").AttributeAs<int>("preordered").AsBool(),
							ForTrade = item.Element("status").AttributeAs<int>("fortrade").AsBool(),
							PreviousOwned = item.Element("status").AttributeAs<int>("prevowned").AsBool(),
							Want = item.Element("status").AttributeAs<int>("want").AsBool(),
							WantToBuy = item.Element("status").AttributeAs<int>("wanttobuy").AsBool(),
							WantToPlay = item.Element("status").AttributeAs<int>("wanttoplay").AsBool(),
							WishList = item.Element("status").AttributeAs<int>("wishlist").AsBool(),

							UserComment = item.Element("comment").As<string>(),

						};

			return items.ToList();
		}

		private int? ParseRanking(XElement ratings)
		{
			string value = (from rank in ratings.Element("ranks").Elements("rank")
							where rank.Attribute("id").Value == "1"
							select rank.Attribute("value").Value).SingleOrDefault();
			if (value == null)
			{
				return null;
			}
			else if (value.ToLower().Trim() == "not ranked")
			{
				return null;
			}

			int ranking;
			if (!int.TryParse(value, out ranking))
			{
				return null;
			}
			return ranking;
		}

		public async Task<List<PlayItem>> GetPlays(string username)
		{

			var url = new Url(BaseUrl).AppendPathSegment("/plays").SetQueryParams(new
			{
				username = username,
				page = 1,
				subtype = "boardgame",
				excludesubtype = "videogame"
			});

			var dataPages = new List<XDocument>();
			dataPages.Add(await DownloadData(url.ToString()));
			var totalPlays = dataPages[0].Element("plays").AttributeAs<int>("total");
			if (totalPlays > 100)
			{
				int remaining = totalPlays - 100;
				int page = 2;
				while (remaining > 0)
				{
					url.SetQueryParam("page", page);
					dataPages.Add(await DownloadData(url.ToString()));
					page++;
					remaining -= 100;
				}
			}

			var plays = new List<PlayItem>();

			foreach (var data in dataPages)
			{
				plays.AddRange(from play in data.Element("plays").Elements("play")
						  select new PlayItem
						  {
							  GameId = play.Element("item").AttributeAs<string>("objectid"),
							  Name = play.Element("item").AttributeAs<string>("name"),
							  PlayDate = ParseDate(play.AttributeAs<string>("date")),
							  NumPlays = play.AttributeAs<int>("quantity"),
							  Comments = play.Element("comments").As<string>()
						  });
			}

			return plays;


		}

		private DateTime? ParseDate(string value)
		{
			DateTime date;
			if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
			{
				return null;
			}
			return date;
		}

		public async Task<GameDetails> GetGame(string gameId)
		{
			var url = new Url(BaseUrl).AppendPathSegment("/thing").SetQueryParams(new
			{
				id = gameId,
				stats = 1
			});

			var data = await DownloadData(url.ToString());

			var game = from item in data.Element("items").Elements("item")
					   select new GameDetails
					   {
						   GameId = item.AttributeAs<string>("id"),
						   Name = (from name in item.Elements("name")
								   where name.AttributeAs<string>("type") == "primary"
								   select name).FirstOrDefault().AttributeAs<string>("value"),
						   Description = item.Element("description").As<string>(),
						   Image = item.Element("image").As<string>(),
						   Thumbnail = item.Element("thumbnail").As<string>(),

						   MinPlayers = item.Element("minplayers").AttributeAs<int?>("value"),
						   MaxPlayers = item.Element("maxplayers").AttributeAs<int?>("value"),
						   PlayingTime = item.Element("playingtime").AttributeAs<int?>("value"),
						   Mechanics = (from link in item.Elements("link")
										where link.AttributeAs<string>("type") == "boardgamemechanic"
										select link.AttributeAs<string>("value")).ToList(),

						   IsExpansion = (from link in item.Elements("link")
										  where link.AttributeAs<string>("type") == "boardgamecategory"
											&& link.AttributeAs<string>("id") == "1024"
										  select link).FirstOrDefault() != null,
						   YearPublished = item.Element("yearpublished").AttributeAs<int?>("value"),

						   BggRating = item.Element("statistics").Element("ratings").Element("bayesaverage").AttributeAs<decimal?>("value"),
						   AverageRating = item.Element("statistics").Element("ratings").Element("average").AttributeAs<decimal?>("value"),
						   Rank = ParseRanking(item.Element("statistics").Element("ratings")),
						   AverageWeight = item.Element("statistics").Element("ratings").Element("averageweight").AttributeAs<decimal?>("value"),

						   Designers = (from link in item.Elements("link")
										where link.AttributeAs<string>("type") == "boardgamedesigner"
										select link.AttributeAs<string>("value")).ToList(),
						   Publishers = (from link in item.Elements("link")
										 where link.AttributeAs<string>("type") == "boardgamepublisher"
										 select link.AttributeAs<string>("value")).ToList(),
						   Artists = (from link in item.Elements("link")
									  where link.AttributeAs<string>("type") == "boardgameartist"
									  select link.AttributeAs<string>("value")).ToList(),

						   Expansions = (from link in item.Elements("link")
										 where link.AttributeAs<string>("type") == "boardgameexpansion"
											&& link.AttributeAs<bool>("inbound") == false
										 select new BoardGameLink
										 {
											 Name = link.AttributeAs<string>("value"),
											 GameId = link.AttributeAs<string>("id")
										 }).ToList(),
						   Expands = (from link in item.Elements("link")
									  where link.AttributeAs<string>("type") == "boardgameexpansion"
										 && link.AttributeAs<bool>("inbound") == true
									  select new BoardGameLink
									  {
										  Name = link.AttributeAs<string>("value"),
										  GameId = link.AttributeAs<string>("id")
									  }).ToList()
					   };

			return game.FirstOrDefault();

		}

		public async Task<GeekList> GetGeekList(string id)
		{
			var url = new Url(LegacyBaseUrl).AppendPathSegment("/geeklist/").AppendPathSegment(id);
			var data = await DownloadData(url.ToString());

			var gl = data.Element("geeklist");

			return new GeekList
			{
				GeekListId = id,
				Username = gl.Element("username").As<string>(),
				Title = gl.Element("title").As<string>(),
				Description = gl.Element("description").As<string>(),
				Items = (from item in gl.Elements("item")
						 where item.AttributeAs<string>("objecttype") == "thing" && item.AttributeAs<string>("subtype") == "boardgame"
						 select new GeekListItem
						 {
							 GameId = item.AttributeAs<string>("objectid"),
							 ImageId = item.AttributeAs<string>("imageid"),
							 Username = item.AttributeAs<string>("username"),
							 Name = item.AttributeAs<string>("objectname"),
							 Description = item.AttributeAs<string>("body")
						 }).ToList()
			};

		}

		#region BGG Download Helpers

		private async Task<XDocument> DownloadData(string url)
		{
			await _semaphore.WaitAsync();
			try
			{
				await WaitForMinimumTimeToPass();
				Debug.WriteLine("Downloading (Async) " + url);
				XDocument data = null;
				int retries = 0;

				try
				{
					while (data == null && retries < 60)
					{
						retries++;
						var request = WebRequest.CreateHttp(url);
						request.Timeout = 15000;
						using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
						{
							if (response.StatusCode == HttpStatusCode.Accepted)
							{
								Debug.WriteLine("Download isn't ready.  Trying again in a moment...");

								//
								// this whole section of playing with the semaphore inside the try/finally 
								// seems dangerous, but I'm doing it anyway...
								//

								// log the end of our last attempt
								ResetMinimumTimeTracker();

								// let other queued up requests happen...
								_semaphore.Release();

								// very small delay to really make sure other requests get the lock
								await Task.Delay(50);

								// get back in line for the lock before continuing
								await _semaphore.WaitAsync();

								// do the real delay now that we have the lock again
								await WaitForMinimumTimeToPass();

								continue;
							}
							using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
							{
								data = XDocument.Parse(await reader.ReadToEndAsync());
							}
						}
					}
				}
				finally
				{

				}
				if (data != null)
				{
					return data;
				}
				else
				{
					throw new Exception("Failed to download BGG data.");
				}
			}
			finally
			{
				ResetMinimumTimeTracker();
				_semaphore.Release();
			}

		}

		private static void ResetMinimumTimeTracker()
		{
			_lastDownloadCompleted = DateTimeOffset.Now;
		}

		private static async Task WaitForMinimumTimeToPass()
		{
			var now = DateTimeOffset.Now;
			var timeSinceLastDownload = now - _lastDownloadCompleted;
			if (timeSinceLastDownload < MinimumTimeBetweenDownloads)
			{
				var requiredDelay = MinimumTimeBetweenDownloads - timeSinceLastDownload;
				Debug.WriteLine("Pausing {0} ms", requiredDelay.TotalMilliseconds);
				await Task.Delay(requiredDelay);
			}
		}

		#endregion

	}
}