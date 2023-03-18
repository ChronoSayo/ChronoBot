using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Helpers;
using ChronoBot.Modules.Tools;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using TwitchLib.Communication.Interfaces;

namespace ChronoBot.Utilities.Tools
{
    public class Deadline
    {
        protected readonly DiscordSocketClient Client;
        protected readonly ReminderFileSystem FileSystem;
        protected readonly List<ReminderUserData> Users;

        public Deadline(DiscordSocketClient client, ReminderFileSystem fileSystem, List<ReminderUserData> users)
        {
            Client = client;
            FileSystem = fileSystem;
            Users = users; var timer = new Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = 1000
            };
            timer.Elapsed += DeadlineCheck;
        }

        protected virtual async void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
        }

        public virtual bool SetDeadline(string message, DateTime dateTime, ulong guildId, ulong channelId, string user, ulong userId)
        {
            return CreateDeadlineUserData(message, dateTime, guildId, channelId, user, userId);
        }

        private bool CreateDeadlineUserData(string message, DateTime dateTime, ulong guildId, ulong channelId, string user, ulong userId)
        {
            ReminderUserData temp = new ReminderUserData
            {
                Name = user,
                GuildId = guildId,
                ChannelId = channelId,
                Deadline = dateTime,
                Id = message,
                UserId = userId
            };
            Users.Add(temp);

            bool ok = FileSystem.Save(temp);
            if (!ok)
                Users.Remove(temp);

            return ok;
        }
    }
}
