using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using InstaSharper.API;
using InstaSharper.API.Builder;
using InstaSharper.Classes;
using InstaSharper.Classes.Models;

namespace ChronoBot.SocialMedias
{
    class Instagram : SocialMedia
    {
        private UserSessionData _userSession;
        private IInstaApi _api;

        public Instagram(DiscordSocketClient client) : base(client)
        {
            _client = client;

            Authenticate();

            //SetCommands("twitter");
            //_hyperlink = "https://twitter.com/@name/status/@id";

            //UpdateTimer(60);//1 minute.

            //_howToMessage = _howToMessage.Replace(_USER_KEYWORD, "Twitter user");

            //LoadOrCreateFromFile("twitternames");
        }

        protected override void UpdateTimer(int seconds)
        {
            base.UpdateTimer(seconds);
        }

        protected override void PostUpdate()
        {
            if (_users.Count == 0)
                return;

            //UpdateSocialMedia(newTweets);
        }
        
        protected override void AddSocialMediaUser(SocketMessage socketMessage)
        {
        }

        protected override void DeleteSocialMediaUser(SocketMessage socketMessage, bool altCommand = false)
        {
            base.DeleteSocialMediaUser(socketMessage, altCommand);
        }

        protected override void GetSocialMediaUser(SocketMessage socketMessage)
        {
        }

        protected override void ListSavedSocialMediaUsers(SocketMessage socketMessage)
        {
            if (ListCommand(socketMessage.ToString()))
            {
                if (_users.Count == 0)
                {
                    Info.Shrug(socketMessage);
                    return;
                }

                string line = "";
                ulong guildID = Info.DEBUG_GUILD_ID;
                for (int i = 0; i < _users.Count; i++)
                {
                    bool addToList = false;
                    if (Info.DEBUG)
                        addToList = true;
                    else
                    {
                        guildID = Info.GetGuildIDFromSocketMessage(socketMessage);
                        addToList = guildID == _users[i].guildID;
                    }
                    if (addToList)
                    {
                        SocketTextChannel channel = _client.GetGuild(guildID).GetTextChannel(_users[i].channelID);
                        string name = _users[i].name;
                        //line += "■ " + SearchTwitterUser(name).Name + " (" + name + ") " +
                        line += "■ " + name + " " +
                            (channel == null ? "***Missing channel info.***" : channel.Mention) + "\n";
                    }
                }
                if (line == "")
                    Info.Shrug(socketMessage);
                else
                    socketMessage.Channel.SendMessageAsync(line);
            }
        }

        private bool MessageDisplayed(string id, ulong guildID)
        {
            foreach (UserData ud in _users)
            {
                if (ud.id == id && guildID == ud.guildID)
                    return true;
            }
            return false;
        }

        private void Authenticate()
        {
            _userSession = new UserSessionData();
            _userSession.UserName = "dawnkeebals9001";
            _userSession.Password = "horunge666";

            _api = InstaApiBuilder.CreateBuilder().SetUser(_userSession).SetRequestDelay(RequestDelay.FromSeconds(8, 10)).Build();
            var login = _api.LoginAsync().Result;
            if(login.Succeeded)
            {
                
                var media = _api.GetUserMediaAsync("chronosayo", PaginationParameters.MaxPagesToLoad(1)).GetAwaiter().GetResult();
                List<InstaMedia> list = media.Value.ToList();

                foreach (var m in list)
                {
                    foreach (var i in m.Images)
                        Info.SendMessageToChannel(Info.DEBUG_GUILD_ID, Info.DEBUG_CHANNEL_ID, i.URI);
                }
            }
        }

        public override void MessageReceived(SocketMessage socketMessage)
        {
            base.MessageReceived(socketMessage);
        }
    }
}
