using System;
using System.Collections.Generic;
using System.Timers;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Helpers;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Utilities.Tools
{
    public class Reminder
    {
        DiscordSocketClient _client;
        private readonly IConfiguration _config;
        private readonly ReminderFileSystem _fileSystem;
        private Timer _timer;
        private readonly List<ReminderUserData> _users;

        public Reminder(DiscordSocketClient client, IConfiguration config, ReminderFileSystem fileSystem)
        {
            _client = client;
            _config = config;
            _fileSystem = fileSystem;
            _users = (List<ReminderUserData>)_fileSystem.Load();

            _timer = new Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = 1000
            };
            _timer.Elapsed += DeadlineCheck;
        }

        private async void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            foreach (ReminderUserData user in _users)
            {
                if (DateTime.Now < user.Deadline)
                    continue;

                string message = $"{user.Name}\n{user.Id}";
                if (Statics.Debug)
                    await Statics.DebugSendMessageToChannelAsync(_client, message);
                else
                    await _client.GetGuild(user.GuildId).GetTextChannel(user.ChannelId).SendMessageAsync(message);
            }
        }

        public bool SetReminder(string message, DateTime dateTime, string where, ulong guildId, ulong channelId, ulong user)
        {
            return CreateReminderUserData(message, dateTime, where, guildId, channelId, user, false);
        }

        public bool SetReminderCountdown(string message, DateTime dateTime, string where, ulong guildId, ulong channelId, ulong user)
        {
            return CreateReminderUserData(message, dateTime, where, guildId, channelId, user, true);
        }

        private bool CreateReminderUserData(string message, DateTime dateTime, string where, ulong guildId, ulong channelId, ulong user, bool countdown)
        {
            ReminderUserData temp = new ReminderUserData
            {
                Name = user.ToString(),
                GuildId = guildId,
                ChannelId = channelId,
                Deadline = dateTime,
                Id = message,
                Countdown = countdown
            };
            _users.Add(temp); 
            
            bool ok = _fileSystem.Save(temp);
            if (!ok)
                _users.Remove(temp);

            return ok;
        }
    }
}
