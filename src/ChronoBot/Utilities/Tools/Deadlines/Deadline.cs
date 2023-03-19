using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using ChronoBot.Enums;

namespace ChronoBot.Utilities.Tools.Deadlines
{
    public class Deadline
    {
        protected readonly DiscordSocketClient Client;
        protected readonly DeadlineFileSystem FileSystem;
        protected List<DeadlineUserData> Users;

        public Deadline(DiscordSocketClient client, DeadlineFileSystem fileSystem, IEnumerable<DeadlineUserData> users)
        {
            Client = client;
            FileSystem = fileSystem;
            Users = users.ToList();
            var timer = new Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = 1000
            };
            timer.Elapsed += DeadlineCheck;
        }

        protected virtual void LoadOrCreateFromFile()
        {
            Users = FileSystem.Load().Cast<DeadlineUserData>().ToList();
        }

        protected virtual async void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            await Task.CompletedTask;
        }

        public virtual DeadlineUserData SetDeadline(string message, DateTime dateTime, ulong guildId, ulong channelId, string user, ulong userId)
        {
            return null;
        }

        protected virtual DeadlineUserData CreateDeadlineUserData(string message, DateTime dateTime, ulong guildId, 
            ulong channelId, string user, ulong userId, DeadlineEnum deadlineType)
        {
            DeadlineUserData temp = new DeadlineUserData
            {
                Name = user,
                GuildId = guildId,
                ChannelId = channelId,
                Deadline = dateTime,
                Id = message,
                UserId = userId,
                DeadlineType = deadlineType
            };
            Users.Add(temp);

            bool ok = FileSystem.Save(temp);
            if (!ok)
                Users.Remove(temp);

            return ok ? temp : null;
        }
    }
}
