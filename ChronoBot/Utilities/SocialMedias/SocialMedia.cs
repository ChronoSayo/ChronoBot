using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Utilities.SocialMedias
{
    public class SocialMedia
    {
        protected readonly IConfiguration Configuration;

        public SocialMedia(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public virtual async Task<string> Test()
        {
            return null;
        }
    }
}
