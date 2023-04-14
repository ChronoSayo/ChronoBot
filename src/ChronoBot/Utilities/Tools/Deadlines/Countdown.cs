using ChronoBot.Common.Systems;
using ChronoBot.Helpers;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using ChronoBot.Modules.Tools.Deadlines;
using ChronoBot.Enums;
using ChronoBot.Common.UserDatas;

namespace ChronoBot.Utilities.Tools.Deadlines
{
    public sealed class Countdown : Deadline
    {
        public Countdown(DiscordSocketClient client, DeadlineFileSystem fileSystem, IEnumerable<DeadlineUserData> users, int seconds = 60) :
            base(client, fileSystem, users, seconds)
        {
        }

        protected override void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            List<DeadlineUserData> countedDownUsers = new List<DeadlineUserData>();
            foreach (DeadlineUserData user in Users)
            {
                if (user.DeadlineType != DeadlineEnum.Countdown)
                    continue;
                
                int daysLeft = TotalDaysLeft(user.Deadline);
                if(user.DaysLeft == daysLeft)
                    continue;

                string message = user.Id;
                if (daysLeft > 0)
                {
                    message =
                        $"***{daysLeft} day" + (daysLeft > 1 ? "s" : string.Empty) + $"** left until:*\n{message}";

                    user.DaysLeft = daysLeft;
                    FileSystem.UpdateFile(user);
                }
                else 
                    countedDownUsers.Add(user);

                SendMessage(user, message);
            }
            
            RemoveUsers(countedDownUsers);
        }
    }
}
