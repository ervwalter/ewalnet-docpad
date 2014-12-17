using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lokad.Cloud.Storage;
using System.Configuration;
using GamesDataProvider;
using System.Threading;

namespace CacheUpdateJob
{
	// To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
	class Program
	{
		static void Main()
		{
			Console.WriteLine();
			Console.WriteLine("Starting update.");

			UpdateGames();
			UpdatePlays();
			UpdateCollections();

			Console.WriteLine();
			Console.WriteLine("Completed update.");
			Console.WriteLine();
		}


		private static void UpdateCollections()
		{
			Console.WriteLine();
			Console.WriteLine("Updating collections.");
			var provider = new BggDataProvider();
			var table = CacheManager.GetTable<Collection>();
			var cutoff = DateTime.UtcNow.AddMinutes(-10);
			var entities = table.Get().ToList();
			var outdated = entities.Where(e => e.Timestamp < cutoff).ToList();
			Console.WriteLine("Found {0} collections, {1} needing updates.", entities.Count, outdated.Count);

			foreach (var entity in outdated)
			{
				Console.WriteLine("Updating collection for {0}.", entity.Value.Username);
				var collection = provider.GetCollection(entity.Value.Username).Result;
				table.Upsert(new CloudEntity<Collection>
				{
					PartitionKey = entity.PartitionKey,
					RowKey = entity.RowKey,
					Value = collection
				});
				Thread.Sleep(2000); // slow down the queries to make sure we don't hit the bgg throttling code
			}
		}

		private static void UpdatePlays()
		{
			Console.WriteLine();
			Console.WriteLine("Updating recent plays.");
			var provider = new BggDataProvider();
			var table = CacheManager.GetTable<Plays>();
			var cutoff = DateTime.UtcNow.AddMinutes(-10);
			var entities = table.Get().ToList();
			var outdated = entities.Where(e => e.Timestamp < cutoff).ToList();
			Console.WriteLine("Found {0} recent play lists, {1} needing updates.", entities.Count, outdated.Count);

			foreach (var entity in outdated)
			{
				Console.WriteLine("Updating recent plays for {0}.", entity.Value.Username);
				var plays = provider.GetPlays(entity.Value.Username).Result;
				table.Upsert(new CloudEntity<Plays>
				{
					PartitionKey = entity.PartitionKey,
					RowKey = entity.RowKey,
					Value = plays
				});
				Thread.Sleep(2000); // slow down the queries to make sure we don't hit the bgg throttling code
			}
		}

		private static void UpdateGames()
		{
			Console.WriteLine();
			Console.WriteLine("Updating games details.");
			var provider = new BggDataProvider();
			var table = CacheManager.GetTable<GameDetails>();
			var cutoff = DateTime.UtcNow.AddHours(-6);
			var entities = table.Get().ToList();
			var outdated = entities.Where(e => e.Timestamp < cutoff).ToList();
			Console.WriteLine("Found {0} games, {1} needing updates.", entities.Count, outdated.Count);

			foreach (var entity in outdated)
			{
				Console.WriteLine("Updating game details for {0} [{1}].", entity.Value.Name, entity.Value.GameId);
				var details = provider.GetGame(entity.RowKey).Result;
				table.Upsert(new CloudEntity<GameDetails>
				{
					PartitionKey = entity.PartitionKey,
					RowKey = entity.RowKey,
					Value = details
				});
				Thread.Sleep(2000); // slow down the queries to make sure we don't hit the bgg throttling code
			}
		}
	}
}
