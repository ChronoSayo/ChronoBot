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

namespace ChronoBot.Utilities.Tools
{
    public class Countdown
    {
        private readonly DiscordSocketClient _client;
        private readonly ReminderFileSystem _fileSystem;
        private readonly List<ReminderUserData> _users;

        public Countdown(DiscordSocketClient client, ReminderFileSystem fileSystem)
        {
            _client = client;
            _fileSystem = fileSystem;
            _users = (List<ReminderUserData>)_fileSystem.Load();

            var timer = new Timer()
            {
                AutoReset = true,
                Enabled = true,
                Interval = 1000
            };
            timer.Elapsed += DeadlineCheck;
        }

        private async void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            List<ReminderUserData> remindedUsers = new List<ReminderUserData>();
            foreach (ReminderUserData user in _users)
            {
                if (now < user.Deadline)
                    continue;

                try
                {
                    var embed = ReminderModule.RemindMessage(user.Id,
                        user.Name,
                        user.Deadline,
                        user.GuildId,
                        user.ChannelId,
                        _client);

                    if (Statics.Debug)
                        await Statics.DebugSendMessageToChannelAsync(_client, embed);
                    else
                        await _client.GetGuild(user.GuildId).GetTextChannel(user.ChannelId).SendMessageAsync(embed: embed);
                }
                catch
                {
                    // Continue
                }

                remindedUsers.Add(user);
            }

            foreach (ReminderUserData user in remindedUsers)
            {
                bool ok = _fileSystem.DeleteInFile(user);
                if (ok)
                    _users.Remove(user);
            }
        }

        public bool SetCountdown(string message, DateTime dateTime, ulong guildId, ulong channelId, string user, ulong userId)
        {
            return CreateReminderUserData(message, dateTime, guildId, channelId, user, userId);
        }

        private bool CreateCountdownUserData(string message, DateTime dateTime, ulong guildId, ulong channelId, string user, ulong userId)
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
            _users.Add(temp);

            bool ok = _fileSystem.Save(temp);
            if (!ok)
                _users.Remove(temp);

            return ok;
        }
    }
}
