using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Helpers;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Timers;
using ChronoBot.Modules.Tools.Deadlines;
using ChronoBot.Enums;

namespace ChronoBot.Utilities.Tools.Deadlines
{
    public sealed class Countdown : Deadline
    {
        public static char Key = '€';

        public Countdown(DiscordSocketClient client, DeadlineFileSystem fileSystem, IEnumerable<DeadlineUserData> users) :
            base(client, fileSystem, users)
        {
            LoadOrCreateFromFile();
        }

        public override DeadlineUserData SetDeadline(string message, DateTime dateTime, ulong guildId, ulong channelId, string user,
            ulong userId)
        {
            return CreateDeadlineUserData(message, dateTime, guildId, channelId, user, userId, DeadlineEnum.Countdown);
        }

        override 

        protected override async void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            List<DeadlineUserData> countdownUsers = new List<DeadlineUserData>();
            foreach (DeadlineUserData user in Users)
            {
                if (user.DeadlineType != DeadlineEnum.Countdown)
                    continue;
                
                string daysLeftInMessage = user.Id.Split(Key)[^1];
                int daysLeft = Convert.ToInt32((user.Deadline - now).TotalDays); 

                if(int.TryParse(daysLeftInMessage, out int days) && daysLeft == days)
                    continue;

                if (user.Id.Contains($"{Key}{daysLeftInMessage}"))
                    user.Id = user.Id.Replace($"{Key}{daysLeftInMessage}", string.Empty);
                string message = user.Id;
                
                if (daysLeft > 0)
                {
                    message =
                        $"***{daysLeft} day" + (daysLeft > 1 ? "s" : string.Empty) + $"** left until:*\n{message}";

                    user.Id += $"{Key}{daysLeft}";
                }
                else 
                    countdownUsers.Add(user);

                try
                {
                    var embed = DeadlineModule.DeadlineEmbed(user, message, Client);
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

            foreach (DeadlineUserData user in countdownUsers)
            {
                bool ok = FileSystem.DeleteInFile(user);
                if (ok)
                    Users.Remove(user);
            }
        }
    }
}
