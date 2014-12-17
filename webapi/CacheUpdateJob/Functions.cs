using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace CacheUpdateJob
{
	public class Functions
	{
		[NoAutomaticTrigger]
		public static void UpdateCache()
		{
			Console.WriteLine("Starting update.");

			// do stuff

			Console.WriteLine("Completed update.");
		}

	}
}
