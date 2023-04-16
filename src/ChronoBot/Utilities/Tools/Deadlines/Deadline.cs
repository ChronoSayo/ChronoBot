using ChronoBot.Common.Systems;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using ChronoBot.Enums;
using Discord;
using ChronoBot.Common.UserDatas;
using ChronoBot.Helpers;
using ChronoBot.Modules.Tools.Deadlines;

namespace ChronoBot.Utilities.Tools.Deadlines
{
    public class Deadline
    {
        protected readonly DiscordSocketClient Client;
        protected readonly DeadlineFileSystem FileSystem;
        protected List<DeadlineUserData> Users;

        public Deadline(DiscordSocketClient client, DeadlineFileSystem fileSystem, IEnumerable<DeadlineUserData> users, int seconds = 60)
        {
            Client = client;
            FileSystem = fileSystem;
            Users = users.ToList();
            var timer = new Timer
            {
                AutoReset = true,
                Enabled = true,
                Interval = seconds * 1000
            };
            timer.Elapsed += DeadlineCheck;

            LoadOrCreateFromFile();
        }

        private void LoadOrCreateFromFile()
        {
            Users = FileSystem.Load().Cast<DeadlineUserData>().ToList();
        }

        protected virtual void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
        }

        protected virtual int TotalDaysLeft(DateTime deadline)
        {
            return Convert.ToInt32((deadline.Date - DateTime.Now.Date).TotalDays);
        }

        public virtual DeadlineUserData SetDeadline(string message, DateTime dateTime, ulong guildId, ulong channelId,
            string user, ulong userId, DeadlineEnum type)
        {
            DateTime setDateTime;
            switch (type)
            {
                case DeadlineEnum.Reminder:
                    setDateTime = dateTime;
                    break;
                case DeadlineEnum.Countdown:
                case DeadlineEnum.Repeater:
                    setDateTime = dateTime.Date;
                    break;
                default:
                    setDateTime = DateTime.Now;
                    break;
            }
            return CreateDeadlineUserData(message, setDateTime, guildId, channelId, user, userId, type,
                TotalDaysLeft(dateTime));
        }

        public virtual DeadlineUserData SetRepeater(string message, DayOfWeek day, ulong guildId, ulong channelId,
            string user, ulong userId, DeadlineEnum type)
        {
            int daysLeft = -(int)DateTime.Today.DayOfWeek + (int)day;
            DateTime dateTime = DateTime.Now.AddDays(daysLeft);
            if (daysLeft < 0)
                dateTime = dateTime.AddDays(7);
            return CreateDeadlineUserData(message, dateTime, guildId, channelId, user, userId, type,
                TotalDaysLeft(dateTime));
        }

        public virtual string GetDeadlines(ulong guildId, ulong channelId, ulong userId, int num, string username,
            string channelName, DeadlineEnum type, out Embed embed)
        {
            embed = null;
            var getEntries = Users.FindAll(x =>
                x.GuildId == guildId && x.ChannelId == channelId && x.UserId == userId && x.DeadlineType == type);

            if (getEntries.Count == 0)
                return $"Nothing found in {channelName}.";
            int i = num - 1;
            if (i >= getEntries.Count)
                return $"Entry number {num} not found.";

            var ud = getEntries[i];
            string deadlineType = ud.DeadlineType.ToString().ToUpper();
            string message = $"\"{ud.Id}\"";

            embed = new EmbedBuilder()
                .WithAuthor(username)
                .WithDescription(message)
                .WithTitle(deadlineType)
                .WithColor(Color.Red)
                .Build();

            return "ok";
        }

        public virtual string ListDeadlines(ulong guildId, ulong channelId, ulong userId, string username,
            string channelName, DeadlineEnum type, out Embed embed)
        {
            embed = null;
            var getEntries = Users.FindAll(x =>
                x.GuildId == guildId && x.ChannelId == channelId && x.UserId == userId && x.DeadlineType == type);
            if (getEntries.Count == 0)
                return $"Nothing found in {channelName}.";

            string deadlineType = getEntries[0].DeadlineType.ToString().ToUpper();
            string message = string.Empty;
            for (int i = 0; i < getEntries.Count; i++)
            {
                var ud = getEntries[i];
                message += $"{i + 1}. \"{ud.Id}\" - {ud.Deadline}\n";
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

        public virtual string DeleteDeadline(ulong guildId, ulong channelId, ulong userId, int num, string channelName, DeadlineEnum type)
        {
            var getEntries = Users.FindAll(x =>
                x.GuildId == guildId && x.ChannelId == channelId && x.UserId == userId && x.DeadlineType == type);

            if (getEntries.Count == 0)
                return $"Nothing found in {channelName}.";

            int i = num - 1;
            if (i >= getEntries.Count)
                return $"Entry number {num} not found.";

            bool ok = FileSystem.DeleteInFile(Users[i]);
            if(ok) 
                Users.RemoveAt(i);
                
            return ok ? $"{type} has been deleted." : "Something went wrong with deleting from file.";
        }

        public virtual string DeleteAllInChannelDeadline(ulong guildId, ulong channelId, ulong userId, string channelName, DeadlineEnum type)
        {
            var getEntries = Users.FindAll(x =>
                x.GuildId == guildId && x.ChannelId == channelId && x.UserId == userId && x.DeadlineType == type);

            if (getEntries.Count == 0)
                return $"Nothing found in {channelName}.";

            string typeName = getEntries[0].DeadlineType.ToString().ToLower();

            List<DeadlineUserData> removeDatas = new List<DeadlineUserData>();
            foreach (DeadlineUserData ud in getEntries)
            {
                bool ok = FileSystem.DeleteInFile(ud);
                if(ok)
                    removeDatas.Add(ud);
            }

            foreach (DeadlineUserData ud in removeDatas)
                Users.Remove(ud);

            return $"All {typeName}s have been deleted from {channelName}.";
        }

        public virtual string DeleteAllInGuildDeadline(ulong guildId, ulong userId, string guildName, DeadlineEnum type)
        {
            var getEntries = Users.FindAll(x => x.GuildId == guildId && x.UserId == userId);

            if (getEntries.Count == 0)
                return $"Nothing found in {guildName}.";

            string typeName = getEntries[0].DeadlineType.ToString().ToLower(); 
            
            List<DeadlineUserData> removeDatas = new List<DeadlineUserData>();
            foreach (DeadlineUserData ud in getEntries)
            {
                bool ok = FileSystem.DeleteInFile(ud);
                if (ok)
                    removeDatas.Add(ud);
            }

            foreach (DeadlineUserData ud in removeDatas)
                Users.Remove(ud);

            return $"All {typeName}s have been deleted from {guildName}.";
        }

        protected virtual DeadlineUserData CreateDeadlineUserData(string message, DateTime dateTime, ulong guildId, 
            ulong channelId, string user, ulong userId, DeadlineEnum deadlineType, int daysLeft)
        {
            DeadlineUserData temp = new DeadlineUserData
            {
                Name = user,
                GuildId = guildId,
                ChannelId = channelId,
                Deadline = dateTime,
                Id = message,
                UserId = userId,
                DeadlineType = deadlineType,
                DaysLeft = daysLeft
            };
            Users.Add(temp);

            bool ok = FileSystem.Save(temp);
            if (!ok)
                Users.Remove(temp);

            return ok ? temp : null;
        }

        protected virtual async void SendMessage(DeadlineUserData user, string message = "")
        {
            try
            {
                var embed = DeadlineModule.DeadlineEmbed(user, message == string.Empty ? user.Id : message, Client);
                if (Statics.Debug)
                    await Statics.DebugSendMessageToChannelAsync(Client, embed);
                else
                    await Client.GetGuild(user.GuildId).GetTextChannel(user.ChannelId).SendMessageAsync(embed: embed);
            }
            catch
            {
                // Continue
            }
        }

        protected virtual void RemoveUsers(List<DeadlineUserData> removeDatas)
        {
            foreach (var user in from user in removeDatas let ok = FileSystem.DeleteInFile(user) where ok select user)
                Users.Remove(user);
        }
    }
}
