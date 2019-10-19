using System.Collections.Generic;
using System.Timers;
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

        protected struct UserData
        {
            public string name;
            public ulong guildID;
            public ulong channelID;
            public string id;
        }

        public SocialMedia(DiscordSocketClient client)
        {
            _client = client;
            _users = new List<UserData>();
        }
        
        protected virtual void LoadOrCreateFromFile(string filename)
        {
            _fileSystem = new FileSystem("Memory Card/" + filename, _client);
            List<UserData> updatedList = new List<UserData>();

            if (_fileSystem.CheckFileExists())
            {
                List<string> newList = _fileSystem.Load();
                if (newList.Count > 0)
                {
                    foreach (string s in newList)
                    {
                        string[] split = s.Split(' '); //0: Name. 1: Guild ID. 2: Channel ID. 3. ID.

                        string name = split[0];

                        ulong guildID = Info.DEBUG_CHANNEL_ID;
                        ulong.TryParse(split[1], out guildID);

                        ulong channelID = Info.DEBUG_CHANNEL_ID;
                        ulong.TryParse(split[2], out channelID);

                        string id = split[3];

                        CreateSocialMediaUser(name, guildID, channelID, id, false);
                    }
                }
            }
        }
        
        protected virtual void CreateSocialMediaUser(string name, ulong guildID, ulong channelID, 
            string id, bool saveToFile)
        {
            UserData temp = new UserData
            {
                name = name,
                guildID = guildID,
                channelID = channelID,
                id = id
            };
            _users.Add(temp);

            if (saveToFile)
                _fileSystem.Save(FormatLineToFile(name, guildID, channelID, id));
        }

        protected virtual string FormatLineToFile(string name, ulong guildID, ulong channelID, string id)
        {
            return name + " " + guildID + " " + channelID + " " + id;
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
                    UserData ud = _users[i];
                    string line = _fileSystem.FindLine(FormatLineToFile(ud.name, ud.guildID, ud.channelID, ud.id));

                    Info.SendMessageToChannel(socketMessage, "Successfully deleted " + ud.name);

                    _fileSystem.DeleteLine(line);

                    _users.RemoveAt(i);
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
                ulong guildID = Info.DEBUG ? Info.DEBUG_GUILD_ID : users[i].guildID;
                ulong channelID = Info.DEBUG ? Info.DEBUG_CHANNEL_ID : users[i].channelID;
                //Checks if usedGuildIDs contains anything to see if the IDs has been posted already.
                if (usedGuildIDs.Count > 0)
                {
                    //Checks if both guild and channel ID's has been used.
                    if (UsedID(usedGuildIDs, guildID) && UsedID(usedChannelIDs, channelID))
                        continue;
                }
                string message = string.Empty;
                //Loops through updated social media with the same channel and guild ID.
                for (int j = 0; j < users.Count; j++)
                {
                    //Adds all updated social media within the same server and channel.
                    //Adds them into a string so the bot can post once.
                    if (guildID == users[j].guildID && channelID == users[j].channelID)
                    {
                        //Add more cases here if more social media is added.
                        switch(_socialMedia)
                        {
                            case "!twitch":
                                message += GetStreamerURLAndGame(users[j], api);
                                break;
                            case "!twitter":
                                message += GetTwitterURL(users[j]);
                                break;
                            case "!youtube":
                                message += GetYouTuber(users[j]);
                                break;
                        }
                        usedGuildIDs.Add(guildID);
                        usedChannelIDs.Add(channelID);
                    }
                }
                //Posts the updated social media.
                if (!string.IsNullOrEmpty(message))
                {
                    if (Info.NoGuildID(guildID))
                        Info.SendMessageToUser(_client.GetUser(channelID), message);
                    else
                        _client.GetGuild(guildID).GetTextChannel(channelID).SendMessageAsync(message);
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
        protected virtual string GetStreamerURLAndGame(UserData ud, TwitchAPI api)
        {
            TwitchLib.Api.Models.v5.Channels.Channel info =
                api.Channels.v5.GetChannelByIDAsync(ud.id).GetAwaiter().GetResult();
            return FormatMarkup(ud.name) + " is playing " + info.Game + "\n" + info.Url + "\n\n";
        }

        //Display text for Twitter.
        protected virtual string GetTwitterURL(UserData ud)
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

        protected virtual bool Duplicate(ulong guildID, string name)
        {
            bool duplicate = false;

            foreach (UserData ud in _users)
            {
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
