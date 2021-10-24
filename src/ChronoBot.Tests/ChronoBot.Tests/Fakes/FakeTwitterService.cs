using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Hammock;
using TweetSharp;

namespace ChronoBot.Tests.Fakes
{
    public class FakeTwitterService : TwitterService
    {
        public override TwitterResponse Response => base.Response;

        public override async Task<TwitterAsyncResult<IEnumerable<TwitterStatus>>> ListTweetsOnUserTimelineAsync(ListTweetsOnUserTimelineOptions options)
        {
            if (options.ScreenName == "Fail")
                return null;

            if (options.ScreenName == "NoStatus")
                return new TwitterAsyncResult<IEnumerable<TwitterStatus>>(null, null);

            if (options.ScreenName == "EmptyStatus")
                return new TwitterAsyncResult<IEnumerable<TwitterStatus>>(new List<TwitterStatus>(), null);

            long id = 123456789;
            string idStr = "chirp";
            if (options.ScreenName == "Updated")
            {
                id = 987654321;
                idStr = "kaw-kaw";
            }

            if (options.ScreenName == "NoId")
                idStr = "";

            var extended = new TwitterExtendedEntities
            {
                Media = new List<TwitterExtendedEntity>
                {
                    new() { ExtendedEntityType = TwitterMediaType.Photo }
                }
            };
            if (options.ScreenName == "NotNsfw")
                extended = null;

            var status = new TwitterStatus
            {
                User = new TwitterUser{ScreenName = options.ScreenName},
                ExtendedEntities = extended,
                Id = id,
                IdStr = idStr
            };
            var statuses = new TwitterAsyncResult<IEnumerable<TwitterStatus>>(new[] { status }, null);
            return await Task.FromResult(statuses);
        }

        public override async Task<TwitterAsyncResult<IEnumerable<TwitterUser>>> SearchForUserAsync(SearchForUserOptions options)
        {
            string name = options.Q;
            if (name == "NotFound")
                return null;
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
