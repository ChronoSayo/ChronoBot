using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Interfaces;
using Xunit;

namespace ChronoTwitch.Tests
{
    public class ChronoTwitchTests
    {
        [Fact]
        public void Authenticate_Test()
        {
            var twitch = new ChronoTwitch();

            twitch.Authenticate("clientId", "secret");

            Assert.True(twitch.ClientId == "clientId");
            Assert.True(twitch.Secret == "secret");
        }
    }
}
