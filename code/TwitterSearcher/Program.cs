using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitterUtils;

namespace TwitterSearcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Clear();
            Console.WriteLine("Twitter Searcher");
            Console.WriteLine("-----------------");

            var searchQueries = new Dictionary<string, string>
            {
                //{ "cerveza", "cerveza OR beer OR cerveja OR bière" },
                { "vino", "vino OR wine OR vinho OR vin" },
                { "whisky", "whisky OR whiskey OR uísque" },
                { "vodka", "vodka" },
                { "fernet", "fernet" }
            };
            var geolocations = new[]
            {
                "-36.070559,-65.132268,2500km",
                "5.307503,-56.706703,3500km",
                "40.323962,-74.756002,2300km",
                "46.773343,-122.846604,2200km",
                "66.433255,-151.966204,1000km",
                "22.075772,-102.808852,2000km",
                "-36.070559,-65.132268,2500km",
                "5.307503,-56.706703,3500km"
            };
            var numberOfTweets = 10000;

            foreach(var searchQuery in searchQueries)
            {
                Console.WriteLine("Searching for: {0}", searchQuery);
                foreach(var geoLocation in geolocations)
                {
                    Console.WriteLine("In location: {0}", geoLocation);
                    var searcher = new TwitterApiSearcher();
                    var tweets = searcher.GetTweetsAsync(searchQuery.Value, geoLocation, numberOfTweets).Result;

                    var outputFilePath = string.Format("{0}.txt", searchQuery.Key);
                    File.AppendAllLines(outputFilePath, tweets.ToArray());
                }
            }

            Console.WriteLine();
            Console.WriteLine("Done. Press any key to continue");
            Console.ReadKey();
        }
    }
}
