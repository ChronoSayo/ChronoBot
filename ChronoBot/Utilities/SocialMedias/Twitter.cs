using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TweetSharp;

namespace ChronoBot.Utilities.SocialMedias
{
    public class Twitter : SocialMedia
    {
        private readonly TwitterService _service;

        public Twitter(IConfiguration configuration) : base(configuration)
        {
            _service = new TwitterService(configuration["Tokens:Twitter:ConsumerKey"], configuration["Tokens:Twitter:ConsumerSecret"]);
            _service.AuthenticateWith(configuration["Tokens:Twitter:Token"], configuration["Tokens:Twitter:Secret"]);
        }

        public override async Task<string> Test()
        {
            var tweets = await _service.ListTweetsOnHomeTimelineAsync(new ListTweetsOnHomeTimelineOptions());
            string s = "";
            for(int i = 0; i < 5; i++)
            {
                var tweet = tweets.Value.ElementAt(i);
                s += tweet.ToTwitterUrl() + "\n";
            }

            return s;
        }
    }
}
