using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TweetSharp;

namespace ChronoBot.Tests.Fakes
{
    public class FakeTwitterService : TwitterService
    {
        private int _addLikeCount = 0;

        public override async Task<TwitterAsyncResult<TwitterStatus>> GetTweetAsync(GetTweetOptions options)
        {
            if (options.Id == 3)
                return null;

            if (options.Id == 2)
                return await Task.FromResult(GetFailTweetAsync());

            var url1 = new Uri(
                "https://video.twimg.com/ext_tw_video/1/pu/vid/180x20/swhi5fpAMRc-fzJp.mp4?tag=12");
            var url2 = new Uri(
                "https://video.twimg.com/ext_tw_video/2/pu/vid/1280x720/swhi5fpAMRc-fzJp.mp4?tag=12");
            var url3 = new Uri(
                "https://video.twimg.com/ext_tw_video/3/pu/vid/12x720/swhi5fpAMRc-fzJp.mp4?tag=12");

            var variants = new List<TwitterMediaVariant>
            {
                new()
                {
                    BitRate = 1, ContentType = "video/mp4", Url = url1
                },
                new()
                {
                    BitRate = 3, ContentType = "video/mp4", Url = url2
                },
                new()
                {
                    BitRate = 2, ContentType = "video/mp4", Url = url3
                }
            };

            var aspectRatio = new List<int> {16, 2};
            var videoInfo = new TwitterVideoInfo()
            {
                Variants = variants,
                AspectRatio = aspectRatio,
                DurationMs = 1230
            };

            var extended = new TwitterExtendedEntities
            {
                Media = new List<TwitterExtendedEntity>
                {
                    new()
                    {
                        ExtendedEntityType = options.Id == 4 ? TwitterMediaType.AnimatedGif : TwitterMediaType.Video,
                        VideoInfo = videoInfo
                    }
                }
            };

            var status = new TwitterStatus
            {
                ExtendedEntities = options.Id == 5 ? null : extended
            };

            var statuses = new TwitterAsyncResult<TwitterStatus>(status, null);
            return await Task.FromResult(statuses);
        }

        private TwitterAsyncResult<TwitterStatus> GetFailTweetAsync()
        {
            var url1 = new Uri(
                "https://video.twimg.com/ext_tw_video/1/pu/vid/ggs/swhi5fpAMRc-fzJp.mp4?tag=12");

            var variants = new List<TwitterMediaVariant>
            {
                new()
                {
                    ContentType = "video/mp4", Url = url1
                }
            };

            var aspectRatio = new List<int> { 16, 2 };
            var videoInfo = new TwitterVideoInfo()
            {
                Variants = variants,
                AspectRatio = aspectRatio,
                DurationMs = 1230
            };

            var extended = new TwitterExtendedEntities
            {
                Media = new List<TwitterExtendedEntity>
                {
                    new()
                    {
                        ExtendedEntityType = TwitterMediaType.Video,
                        VideoInfo = videoInfo
                    }
                }
            };

            var status = new TwitterStatus
            {
                ExtendedEntities = extended
            };

            var statuses = new TwitterAsyncResult<TwitterStatus>(status, null);
            return statuses;
        }
        
        public override async Task<TwitterAsyncResult<IEnumerable<TwitterStatus>>> ListFavoriteTweetsAsync(ListFavoriteTweetsOptions options)
        {
            if (options.ScreenName == "NoLike")
                return null;

            TwitterStatus status = null;
            if (options.ScreenName == "AddLike")
            {
                if (_addLikeCount == 0)
                {
                    status = new TwitterStatus
                    {
                        User = new TwitterUser { ScreenName = options.ScreenName },
                        Id = 123456789,
                        IdStr = "123"
                    }; 
                    _addLikeCount++;
                }
                else
                {
                    status = new TwitterStatus
                    {
                        User = new TwitterUser { ScreenName = options.ScreenName },
                        Id = 123456789,
                        IdStr = "987"
                    };
                }
            }
            else 
            {
                status = new TwitterStatus
                {
                    User = new TwitterUser { ScreenName = options.ScreenName },
                    Id = 123456789,
                    IdStr = "123"
                };
            }
            var statuses = new TwitterAsyncResult<IEnumerable<TwitterStatus>>(new[] { status }, null);
            return await Task.FromResult(statuses);
        }

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
                    new() { ExtendedEntityType = TwitterMediaType.AnimatedGif }
                }
            };

            var status = new TwitterStatus
            {
                User = new TwitterUser{ScreenName = options.ScreenName},
                ExtendedEntities = extended,
                Id = id,
                IdStr = idStr,
                IsRetweeted = options.ScreenName == "Tweeter5",
                IsQuoteStatus = options.ScreenName == "Tweeter5",
                CreatedDate = DateTime.Now
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
            if(name == "Multiple")
                users = new TwitterAsyncResult<IEnumerable<TwitterUser>>(new[] { user, user, user, user }, null);
            else if (name == "MultipleSlightlyDifferent")
                users = new TwitterAsyncResult<IEnumerable<TwitterUser>>(
                    new[] { new TwitterUser { ScreenName = "m" }, user }, null);
            return await Task.FromResult(users);
        }

        public override void AuthenticateWith(string consumerKey, string consumerSecret, string token, string tokenSecret)
        {
        }
    }
}
