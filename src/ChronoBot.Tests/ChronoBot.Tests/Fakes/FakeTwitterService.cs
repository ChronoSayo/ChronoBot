using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Hammock;
using TweetSharp;

namespace ChronoBot.Tests.Fakes
{
    public class FakeTwitterService : TwitterService
    {
        public override async Task<TwitterAsyncResult<IEnumerable<TwitterStatus>>> ListTweetsOnUserTimelineAsync(ListTweetsOnUserTimelineOptions options)
        {
            var status = new TwitterStatus
            {
                User = new TwitterUser{ScreenName = options.ScreenName},
                ExtendedEntities = new TwitterExtendedEntities
                {
                    Media = new List<TwitterExtendedEntity>
                    {
                        new() { ExtendedEntityType = TwitterMediaType.Photo }
                    }
                },
                Id = 123456789,
                IdStr = "chirp"
            };
            var statuses = new TwitterAsyncResult<IEnumerable<TwitterStatus>>(new[] { status }, null);
            return await Task.FromResult(statuses);
        }

        public override async Task<TwitterAsyncResult<IEnumerable<TwitterUser>>> SearchForUserAsync(SearchForUserOptions options)
        {
            string name = options.Q;
            var user = new TwitterUser
            {
                ScreenName = name
            };
            var users = new TwitterAsyncResult<IEnumerable<TwitterUser>>(new []{ user }, null);
            return await Task.FromResult(users);
        }

        public override void AuthenticateWith(string consumerKey, string consumerSecret, string token, string tokenSecret)
        {
        }
    }
}
