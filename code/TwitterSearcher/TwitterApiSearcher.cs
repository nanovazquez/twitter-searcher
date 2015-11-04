using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TwitterUtils
{
    public class TwitterApiSearcher
    {
        private static readonly string TwitterConsumerKeyName = "TwitterApiConsumerKey";

        private static readonly string TwitterConsumerSecretName = "TwitterApiConsumerSecret";

        private static readonly TwitterContext Context = null;

        // Static constructor is called at most one time, before any
        // instance constructor is invoked or member is accessed.
        static TwitterApiSearcher()
        {
            Context = InitializeTwitterContextAsync().Result;
        }

        public async Task<IEnumerable<string>> GetTweetsAsync(string searchQuery, string geolocationInfo, int limit = 100, string searchLanguages = "es")
        {
            var windowInMinutes = 15;
            var requestsLimitInWindow = 450;
            var tweetsReceived = 0;
            var numberOfRequests = 0;
            var startTime = DateTime.Now;
            var result = new List<string>();
            long maxId = 0;

            while (tweetsReceived < limit && maxId >= 0)
            {
                if (numberOfRequests < requestsLimitInWindow)
                {
                    try
                    {
                        var searchResponse = 
                            await Context.Search.Where(
                                s => s.Type == SearchType.Search
                                        && s.Count == 100
                                        //&& s.SearchLanguage == searchLanguages
                                        && s.GeoCode == geolocationInfo
                                        && s.MaxID == (ulong)maxId
                                        && s.Query == searchQuery)
                            .SingleOrDefaultAsync()
                            .ConfigureAwait(false);

                        numberOfRequests++;
                        if (searchResponse != null && searchResponse.Statuses != null)
                        {
                            if (searchResponse.SearchMetaData != null && searchResponse.SearchMetaData.NextResults != null)
                            {
                                var nextMaxId = searchResponse.SearchMetaData.NextResults.Split(new string[] { "max_id=" }, StringSplitOptions.RemoveEmptyEntries)[1].Split('&')[0];
                                maxId = Convert.ToInt64(nextMaxId);
                            }
                            else
                            {
                                maxId = -1;
                            }

                            var tweets = searchResponse.Statuses.Select(t => string.Format("\"{0}\",{1},{2},{3}", t.Text, t.User.FriendsCount, t.Place.FullName, geolocationInfo));
                            tweetsReceived += tweets.Count();
                            result.AddRange(tweets);
                        }
                    }
                    catch (Exception e)
                    {
                        if ((e.Message != null && e.Message.Contains("Rate limit exceeded"))
                             || (e.InnerException != null && e.InnerException.Message.Contains("Rate limit exceeded")))
                        {
                            numberOfRequests = requestsLimitInWindow;
                        }else
                        {
                            Console.WriteLine("Unknown error: {0}", e.Message);
                        }
                    }
                }
                else
                {
                    // Request limit exceeded
                    // Wait until request quota is restarted (max 15 minutes)
                    var minutesToWait = windowInMinutes - (DateTime.Now - startTime).Minutes + 1;
                    Console.WriteLine("Request limit exceeded. Waiting {0} minutes (at {1})", minutesToWait, DateTime.Now.ToShortTimeString());
                    Task.Delay(TimeSpan.FromMinutes(minutesToWait)).Wait();
                    numberOfRequests = 0;
                    startTime = DateTime.Now;
                }
            }

            return result;
        }

        private static async Task<TwitterContext> InitializeTwitterContextAsync()
        {
            var auth = new ApplicationOnlyAuthorizer
            {
                CredentialStore = new SingleUserInMemoryCredentialStore
                {
                    ConsumerKey = ConfigurationManager.AppSettings[TwitterConsumerKeyName],
                    ConsumerSecret = ConfigurationManager.AppSettings[TwitterConsumerSecretName],
                }
            };

            await auth.AuthorizeAsync().ConfigureAwait(false);

            return new TwitterContext(auth);
        }
    }
}
