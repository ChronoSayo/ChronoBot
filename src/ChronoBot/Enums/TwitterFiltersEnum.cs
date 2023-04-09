using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ChronoBot.Enums
{
    public class TwitterFiltersEnum
    {
        public enum TwitterFilters
        {
            Posts,
            Retweets,
            Likes,
            QuoteRetweets,
            AllMedia,
            Pictures,
            Gif,
            Video,
            All
        }

        public static string ConvertEnumToFilter(TwitterFilters filter)
        {
            return filter switch
            {
                TwitterFilters.Posts => "p",
                TwitterFilters.Retweets => "q",
                TwitterFilters.Likes => "l",
                TwitterFilters.QuoteRetweets => "q",
                TwitterFilters.AllMedia => "m",
                TwitterFilters.Pictures => "mp",
                TwitterFilters.Gif => "mg",
                TwitterFilters.Video => "v",
                TwitterFilters.All => "",
                _ => ""
            };
        }

        public static TwitterFilters ConvertStringToEnum(string filter)
        {
            return filter switch
            {
                "p" => TwitterFilters.Posts,
                "r" => TwitterFilters.Retweets,
                "l" => TwitterFilters.Likes,
                "q" => TwitterFilters.QuoteRetweets,
                "m" => TwitterFilters.AllMedia,
                "mp" => TwitterFilters.Pictures,
                "mg" => TwitterFilters.Gif,
                "v" => TwitterFilters.Video,
                "" => TwitterFilters.All,
                _ => TwitterFilters.All
            };
        }

        public static List<TwitterFilters> FiltersList()
        {
            return Enum.GetValues<TwitterFilters>().ToList();
        }
    }
}
