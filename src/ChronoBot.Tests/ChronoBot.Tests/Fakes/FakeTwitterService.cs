using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using TweetSharp;

namespace ChronoBot.Tests.Fakes
{
    public class FakeTwitterService : TwitterService
    {
        public override Task<TwitterAsyncResult<IEnumerable<TwitterUser>>> SearchForUserAsync(SearchForUserOptions options)
        {
            var user = new TwitterUser()
            {
                Name = "daigothebeast",
                CreatedDate = DateTime.Now,
                Entities = new TwitterUserProfileEntities()
            };
            var users = new TwitterAsyncResult<IEnumerable<TwitterUser>>(new []{ new TwitterUser()}, null);
            return new Task<TwitterAsyncResult<IEnumerable<TwitterUser>>>(() => users);
        }

        public override void AuthenticateWith(string consumerKey, string consumerSecret, string token, string tokenSecret)
        {
        }
    }
}
