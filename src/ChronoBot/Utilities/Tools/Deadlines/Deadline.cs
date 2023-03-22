using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using ChronoBot.Enums;
using Discord;
using Google.Apis.YouTube.v3.Data;

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

        public virtual string GetDeadlines(ulong guildId, ulong channelId, ulong userId, int num, string username, string channelName, out Embed embed)
        {
            embed = null;
            var getEntries = Users.FindAll(x => x.GuildId == guildId && x.ChannelId == channelId && x.UserId == userId);

            if (getEntries.Count == 0)
                return $"Nothing found in {channelName}.";
            int i = num - 1;
            if (i >= getEntries.Count)
                return $"Entry number {num} not found.";
            
            var ud = getEntries[i];
            string deadlineType = ud.DeadlineType.ToString().ToUpper();
            string message = $"\"{ud.Id}\"";

            if (ud.DeadlineType == DeadlineEnum.Countdown && message.Contains(Countdown.Key))
            {
                string daysLeft = message.Split(Countdown.Key)[^1];
                message = message.Replace($"{Countdown.Key}{daysLeft}", "");
            }

            embed = new EmbedBuilder()
                .WithAuthor(username)
                .WithDescription(message)
                .WithTitle(deadlineType)
                .WithColor(Color.Red)
                .Build();

            return "ok";
        }

        public virtual string ListDeadlines(ulong guildId, ulong channelId, ulong userId, string username, string channelName, out Embed embed)
        {
            embed = null;
            var getEntries = Users.FindAll(x => x.GuildId == guildId && x.ChannelId == channelId && x.UserId == userId);
            if (getEntries.Count == 0)
                return $"Nothing found in {channelName}.";

            string deadlineType = getEntries[0].DeadlineType.ToString().ToUpper();
            string message = string.Empty;
            for (int i = 0; i < getEntries.Count; i++)
            {
                var ud = getEntries[i];
                message += $"{i + 1}. \"{ud.Id}\" - {ud.Deadline} \n";
            }
            message = message.TrimEnd();

            embed = new EmbedBuilder()
                .WithAuthor(username)
                .WithDescription(message)
                .WithTitle(deadlineType)
                .WithColor(Color.Green)
                .Build();

            return "ok";
        }

        public virtual string DeleteDeadline(ulong guildId, ulong channelId, ulong userId, int num, string channelName)
        {
            var getEntries = Users.FindAll(x => x.GuildId == guildId && x.ChannelId == channelId && x.UserId == userId);

            if (getEntries.Count == 0)
                return $"Nothing found in {channelName}.";
            int i = num - 1;
            return i >= getEntries.Count ? $"Entry number {num} not found." : $"Deleted entry number {num}.";
        }

        public virtual string DeleteAllInChannelDeadline(ulong guildId, ulong channelId, ulong userId, string channelName)
        {
            var getEntries = Users.FindAll(x => x.GuildId == guildId && x.ChannelId == channelId && x.UserId == userId);

            if (getEntries.Count == 0)
                return $"Nothing found in {channelName}.";

            string type = getEntries[0].DeadlineType.ToString().ToLower();
            return $"All {type}s have been deleted from {channelName}.";
        }

        public virtual string DeleteAllInGuildDeadline(ulong guildId, ulong userId, string guildName)
        {
            var getEntries = Users.FindAll(x => x.GuildId == guildId && x.UserId == userId);

            if (getEntries.Count == 0)
                return $"Nothing found in {guildName}.";

            string type = getEntries[0].DeadlineType.ToString().ToLower();
            return $"All {type}s have been deleted from {guildName}.";
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
