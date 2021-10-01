﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Helpers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Utilities.SocialMedias
{
    public class SocialMedia
    {
        protected readonly DiscordSocketClient Client;
        protected readonly IConfiguration Config;
        protected Timer UpdateTimer;
        protected SocialMediaFileSystem FileSystem;
        protected string Hyperlink;
        protected string TypeOfSocialMedia;
        protected List<SocialMediaUserData> Users;

        public SocialMedia(IConfiguration config, DiscordSocketClient client)
        {
            Config = config;
            Client = client;
            Users = new List<SocialMediaUserData>();
        }

        protected virtual void LoadOrCreateFromFile()
        {
            FileSystem = new SocialMediaFileSystem();
            Users = FileSystem.Load().Cast<SocialMediaUserData>().ToList();
        }
        
        protected virtual void CreateSocialMediaUser(string name, ulong guildId, ulong channelId, string id, string socialMedia)
        {
            SocialMediaUserData temp = new SocialMediaUserData()
            {
                Name = name,
                GuildId = guildId,
                ChannelId = channelId,
                Id = id,
                SocialMedia = socialMedia
            };
            Users.Add(temp);

            FileSystem.Save(temp);

            //LogToFile(LogSeverity.Info, $"Saved user: {temp.socialMedia} {temp.name} {temp.guildID} {temp.channelID} {temp.id}");
        }

        protected virtual void OnUpdateTimerAsync(int seconds)
        {
            const int toSeconds = 1000;
            int time = seconds * toSeconds;
            UpdateTimer = new Timer(time)
            {
                Enabled = true,
                AutoReset = true
            };
            UpdateTimer.Elapsed += async (s, e) => await UpdateTimerElapsed();
        }

        protected virtual async Task UpdateTimerElapsed()
        {
            await PostUpdate();
        }

        protected virtual async Task PostUpdate()
        {
            await Task.CompletedTask;
        }

        public virtual async Task<string> AddSocialMediaUser(SocketCommandContext context, string username, ulong channelId = 0)
        {
            return await Task.FromResult(string.Empty);
        }

        public virtual string DeleteSocialMediaUser(ulong guildId, string user)
        {
            int i = FindIndexByName(guildId, user);
            if (i > -1)
            {
                SocialMediaUserData ud = Users[i];
                FileSystem.DeleteInFile(ud);
                Users.RemoveAt(i);
                return $"Successfully deleted {ud.Name}";
            }
            return $"Failed to delete {user}";
        }

        public virtual async Task<string> GetSocialMediaUser(SocketCommandContext context, string user)
        {
            return await Task.FromResult(string.Empty);
        }

        public virtual async Task<string> ListSavedSocialMediaUsers(SocketCommandContext context)
        {
            return await Task.FromResult(string.Empty);
        }

        //If more social media is inheriting from this class, add their clients as parameter if needed.
        //protected virtual string UpdateSocialMedia(List<UserData> users, TwitchAPI api = null)
        protected virtual async Task UpdateSocialMedia(IEnumerable<SocialMediaUserData> socialMediaUsers)
        {
            //Save guild ID's and channel ID's to avoid repetition
            List<ulong> usedGuildIDs = new List<ulong>();
            List<ulong> usedChannelIDs = new List<ulong>();
            List<SocialMediaUserData> users = socialMediaUsers.ToList();
            //Loop through all updated social media.
            for (int i = 0; i < users.Count; i++)
            {
                ulong debugGuildId = ulong.Parse(Config["IDs:Guild"]);
                ulong guildId = Statics.Debug ? debugGuildId : users[i].GuildId;
                ulong channelId = Statics.Debug ? Statics.DebugChannelId : users[i].ChannelId;
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
                    if ((guildId == users[j].GuildId && channelId == users[j].ChannelId) || Statics.Debug)
                    {
                        //Add more cases here if more social media is added.
                        switch(TypeOfSocialMedia)
                        {
                            //case "!twitch":
                            //    message += GetStreamerUrlAndGame(users[j], api);
                            //    break;
                            case "twitter":
                                message += GetTwitterUrl(users[j]);
                                break;
                            case "youtube":
                                message += GetYouTuber(users[j]);
                                break;
                        }
                        usedGuildIDs.Add(guildId);
                        usedChannelIDs.Add(channelId);
                    }
                }
                if (string.IsNullOrEmpty(message)) 
                    continue;

                //Posts the updated social media.
                if (Statics.Debug)
                    await Statics.DebugSendMessageToChannelAsync(message); 
                else
                    await Client.GetGuild(guildId).GetTextChannel(channelId).SendMessageAsync(message);

                //UserData user = _users[i];
                //LogToFile($"Updating {user.name} {user.id} {user.channelID} {user.guildID} {user.socialMedia}");
            }
        }

        /// <summary>
        /// Implement social media specific functions here.
        /// </summary>
        /// <param name="socketMessage"></param>
        protected virtual void OtherCommands(SocketMessage socketMessage)
        {

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
        protected virtual string GetStreamerUrlAndGame(SocialMediaUserData ud/*, TwitchAPI api*/)
        {
            //var info =
            //    api.V5.Channels.GetChannelByIDAsync(ud.id).GetAwaiter().GetResult();
            //return FormatMarkup(ud.name) + " is playing " + info.Game + "\n" + info.Url + "\n\n";
            return string.Empty;
        }

        //Display text for Twitter.
        protected virtual string GetTwitterUrl(SocialMediaUserData ud)
        {
            string message = Hyperlink.Replace("@name", ud.Name);
            message = message.Replace("@id", ud.Id);
            return message + "\n\n";
        }

        //Display text for YouTube.
        protected virtual string GetYouTuber(SocialMediaUserData ud)
        {
            string message = Hyperlink + ud.Id;
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

        protected virtual bool Duplicate(ulong guildId, string name, string socialMedia)
        {
            bool duplicate = false;

            foreach (SocialMediaUserData ud in Users)
            {
                if(ud.SocialMedia != socialMedia)
                    continue;

                if (ud.GuildId != guildId ||
                    !string.Equals(ud.Name, name, StringComparison.CurrentCultureIgnoreCase)) 
                    continue;

                duplicate = true;
                break;
            }

            return duplicate;
        }

        protected virtual int FindIndexByName(ulong guildID, string name)
        {
            int index = -1;

            for (int i = 0; i < Users.Count; i++)
            {
                if (Users[i].GuildId != guildID ||
                    !String.Equals(Users[i].Name, name, StringComparison.CurrentCultureIgnoreCase)) 
                    continue;

                index = i;
                break;
            }

            return index;
        }

        //protected virtual void LogToFile(LogSeverity severity, string message, Exception e = null, [CallerMemberName] string caller = null)
        //{
        //    StackTrace st = new StackTrace();
        //    Program.Logger(new LogMessage(severity, st.GetFrame(1).GetMethod().ReflectedType + "." + caller, message, e));
        //}

        //public virtual void MessageReceived(SocketMessage socketMessage)
        //{
        //    MessageReceivedSelf(socketMessage);
        //    if (socketMessage.Author.IsBot)
        //        return;

        //    AddSocialMediaUser(socketMessage);
        //    DeleteSocialMediaUser(socketMessage);
        //    GetSocialMediaUser(socketMessage);
        //    ListSavedSocialMediaUsers(socketMessage);
        //    HowTo(socketMessage);

        //    OtherCommands(socketMessage);
        //}

        public virtual void MessageReceivedSelf(SocketMessage socketMessage)
        {

        }

    }
}