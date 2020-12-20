using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;
using Discord;
using Discord.WebSocket;
using TwitchLib.Api;

namespace ChronoBot
{
    class SocialMedia
    {
        protected DiscordSocketClient _client;
        protected Timer _updateTimer;
        protected FileSystem _fileSystem;
        protected string _hyperlink;
        protected string _addCommand, _deleteCommand, _getCommand, _listCommand, _socialMedia;
        protected string _howToMessage;
        protected List<UserData> _users;
        protected const string _USER_KEYWORD = "%";

        public struct UserData
        {
            public string name;
            public ulong guildID;
            public ulong channelID;
            public string id;
            public string socialMedia;
        }

        public SocialMedia(DiscordSocketClient client)
        {
            _client = client;
            _users = new List<UserData>();
        }
        
        protected virtual void LoadOrCreateFromFile()
        {
            _fileSystem = new FileSystem();
            _users = _fileSystem.Load();
        }
        
        protected virtual void CreateSocialMediaUser(string name, ulong guildId, ulong channelId, string id, string socialMedia)
        {
            UserData temp = new UserData
            {
                name = name,
                guildID = guildId,
                channelID = channelId,
                id = id,
                socialMedia = socialMedia
            };
            _users.Add(temp);

            _fileSystem.Save(temp);

            LogToFile(LogSeverity.Info, $"Saved user: {temp.socialMedia} {temp.name} {temp.guildID} {temp.channelID} {temp.id}");
        }

        protected virtual void SetCommands(string socialMedia)
        {
            _socialMedia = Info.COMMAND_PREFIX + socialMedia;
            _addCommand = _socialMedia + "add";
            _deleteCommand = _socialMedia + "delete";
            _getCommand = _socialMedia + "get";
            _listCommand = _socialMedia + "list";

            _howToMessage = "__HOW TO USE " + socialMedia.ToUpper() + " COMMANDS:__\n***" +
                    _addCommand + " [name] [channel]\n ***" +
                        "-Adds the " + _USER_KEYWORD + ". Tag a channel to update in that channel. " +
                        "If no channel given, updates will be in posted channel.\n***" +
                    _deleteCommand + " [name]\n***" + "-Deletes the " + _USER_KEYWORD + ".\n***" +
                    _getCommand + " [name]\n***" + "-Gets the link to " + _USER_KEYWORD + "'s " +
                        socialMedia[0].ToString().ToUpper() + socialMedia.Substring(1, socialMedia.Length - 1) + ".\n***" +
                    _listCommand + "\n***-Lists all the added " + _USER_KEYWORD + "s in this server.";
        }

        protected virtual void UpdateTimer(int seconds)
        {
            const int TO_SECONDS = 1000;
            int time = Info.DEBUG ? 6 * TO_SECONDS : seconds * TO_SECONDS;
            _updateTimer = new Timer(time)
            {
                Enabled = true,
                AutoReset = true
            };
            _updateTimer.Elapsed += _updateTimer_Elapsed;
        }

        protected virtual void _updateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PostUpdate();
        }

        protected virtual bool HowToCommand(string message)
        {
            return message.ToLower() == _socialMedia;
        }

        protected virtual bool AddCommand(string message)
        {
            return message.ToLower().Contains(_addCommand);
        }

        protected virtual bool DeleteCommand(string message)
        {
            return message.ToLower().Contains(_deleteCommand);
        }

        protected virtual bool GetCommand(string message)
        {
            return message.ToLower().Contains(_getCommand);
        }

        protected virtual bool ListCommand(string message)
        {
            return message.ToLower() == _listCommand;
        }

        protected virtual void PostUpdate()
        {

        }

        protected virtual void HowTo(SocketMessage socketMessage)
        {
            if (HowToCommand(socketMessage.ToString()))
            {
                ulong guildID = Info.DEBUG ? Info.DEBUG_GUILD_ID : Info.GetGuildIDFromSocketMessage(socketMessage);
                ulong channelID = Info.DEBUG ? Info.DEBUG_GUILD_ID : socketMessage.Channel.Id;
                if (Info.NoGuildID(guildID))
                    Info.SendMessageToUser(_client.GetUser(channelID), _howToMessage);
                else
                    _client.GetGuild(guildID).GetTextChannel(channelID).SendMessageAsync(_howToMessage);

            }
        }

        protected virtual void AddSocialMediaUser(SocketMessage socketMessage)
        {

        }

        protected virtual void DeleteSocialMediaUser(SocketMessage socketMessage, bool altCommand = false)
        {
            if (DeleteCommand(socketMessage.ToString()) || altCommand)
            {
                string[] split = socketMessage.ToString().Split(' '); //0: Command. 1: Name.
                if (split.Length == 1)
                {
                    Info.SendMessageToChannel(socketMessage, "Not found.");
                    return;
                }

                int i = FindIndexByName(Info.GetGuildIDFromSocketMessage(socketMessage), split[1]);
                if (i > -1)
                {
                    UserData user = _users[i];
                    _fileSystem.DeleteInFile(user);
                    _users.RemoveAt(i);
                    Info.SendMessageToChannel(socketMessage, "Successfully deleted " + user.name);
                    //LogToFile(new LogMessage(LogSeverity.Info, "" $"Deleted {user.name} {user.id} {user.channelID} {user.guildID} {user.socialMedia}"));
                }
                else
                    Info.SendMessageToChannel(socketMessage, "Failed to delete: " + split[1]);
            }
        }

        protected virtual void GetSocialMediaUser(SocketMessage socketMessage)
        {
        }

        protected virtual void ListSavedSocialMediaUsers(SocketMessage socketMessage)
        {

        }

        //If more social media is inheriting from this class, add their clients as parameter if needed.
        protected virtual void UpdateSocialMedia(List<UserData> users, TwitchAPI api = null)
        {
            //Save guild ID's and channel ID's to avoid repetition
            List<ulong> usedGuildIDs = new List<ulong>();
            List<ulong> usedChannelIDs = new List<ulong>();
            //Loop through all updated social media.
            for (int i = 0; i < users.Count; i++)
            {
                ulong guildId = Info.DEBUG ? Info.DEBUG_GUILD_ID : users[i].guildID;
                ulong channelId = Info.DEBUG ? Info.DEBUG_CHANNEL_ID : users[i].channelID;
                //Checks if usedGuildIDs contains anything to see if the IDs has been posted already.
                if (usedGuildIDs.Count > 0)
                {
                    //Checks if both guild and channel ID's has been used.
                    if (UsedID(usedGuildIDs, guildId) && UsedID(usedChannelIDs, channelId))
                        continue;
                }
                string message = string.Empty;
                //Loops through updated social media with the same channel and guild ID.
                for (int j = 0; j < users.Count; j++)
                {
                    //Adds all updated social media within the same server and channel.
                    //Adds them into a string so the bot can post once.
                    if ((guildId == users[j].guildID && channelId == users[j].channelID) || Info.DEBUG)
                    {
                        //Add more cases here if more social media is added.
                        switch(_socialMedia)
                        {
                            case "!twitch":
                                message += GetStreamerUrlAndGame(users[j], api);
                                break;
                            case "!twitter":
                                message += GetTwitterUrl(users[j]);
                                break;
                            case "!youtube":
                                message += GetYouTuber(users[j]);
                                break;
                        }
                        usedGuildIDs.Add(guildId);
                        usedChannelIDs.Add(channelId);
                    }
                }
                //Posts the updated social media.
                if (!string.IsNullOrEmpty(message))
                {
                    if (Info.NoGuildID(guildId))
                        Info.SendMessageToUser(_client.GetUser(channelId), message);
                    else
                        _client.GetGuild(guildId).GetTextChannel(channelId).SendMessageAsync(message);

                    UserData user = _users[i];
                    //LogToFile($"Updating {user.name} {user.id} {user.channelID} {user.guildID} {user.socialMedia}");
                }
            }
        }

        private bool UsedID(List<ulong> idList, ulong id)
        {
            bool used = false;
            for (int i = 0; i < idList.Count; i++)
            {
                if (id == idList[i])
                {
                    used = true;
                    break;
                }
            }
            return used;
        }

        //Display text for Twitch.
        protected virtual string GetStreamerUrlAndGame(UserData ud, TwitchAPI api)
        {
            var info =
                api.V5.Channels.GetChannelByIDAsync(ud.id).GetAwaiter().GetResult();
            return FormatMarkup(ud.name) + " is playing " + info.Game + "\n" + info.Url + "\n\n";
        }

        //Display text for Twitter.
        protected virtual string GetTwitterUrl(UserData ud)
        {
            string message = _hyperlink.Replace("@name", ud.name);
            message = message.Replace("@id", ud.id);
            return message + "\n\n";
        }

        //Display text for YouTube.
        protected virtual string GetYouTuber(UserData ud)
        {
            string message = _hyperlink + ud.id;
            return message + "\n\n";
        }

        private string FormatMarkup(string name)
        {
            string formatName = name;
            int count = 0;
            int index = -1;
            for(int i = 0; i < formatName.Length; i++)
            {
                if (formatName[i] == '_')
                    count++;
                else
                    count = 0;
                if (count == 2)
                {
                    index = i;
                    break;
                }
            }

            if(count == 2)
                formatName = formatName.Insert(index, "\\");

            return formatName;
        }

        protected virtual bool Duplicate(ulong guildID, string name, string socialMedia)
        {
            bool duplicate = false;

            foreach (UserData ud in _users)
            {
                if(ud.socialMedia != socialMedia)
                    continue;
                
                if (ud.guildID == guildID && ud.name.ToLower() == name.ToLower())
                {
                    duplicate = true;
                    break;
                }
            }

            return duplicate;
        }

        protected virtual int FindIndexByName(ulong guildID, string name)
        {
            int index = -1;

            for (int i = 0; i < _users.Count; i++)
            {
                if (_users[i].guildID == guildID &&
                    _users[i].name.ToLower() == name.ToLower())
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        protected virtual void LogToFile(LogSeverity severity, string message, Exception e = null, [CallerMemberName] string caller = null)
        {
            StackTrace st = new StackTrace();
            Program.Logger(new LogMessage(severity, st.GetFrame(1).GetMethod().ReflectedType + "." + caller, message, e));
        }

        public virtual void MessageReceived(SocketMessage socketMessage)
        {
            MessageReceivedSelf(socketMessage);
            if (socketMessage.Author.IsBot)
                return;

            AddSocialMediaUser(socketMessage);
            DeleteSocialMediaUser(socketMessage);
            GetSocialMediaUser(socketMessage);
            ListSavedSocialMediaUsers(socketMessage);
            HowTo(socketMessage);
        }

        public virtual void MessageReceivedSelf(SocketMessage socketMessage)
        {

        }

    }
}
