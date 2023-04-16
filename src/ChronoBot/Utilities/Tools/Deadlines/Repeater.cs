using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Timers;

namespace ChronoBot.Utilities.Tools.Deadlines
{
    public class Repeater : Deadline
    {
        private const int NextWeek = 7;
        public Repeater(DiscordSocketClient client, DeadlineFileSystem fileSystem, IEnumerable<DeadlineUserData> users, int seconds = 5) :
            base(client, fileSystem, users, seconds)
        {
        }

        protected override void DeadlineCheck(object sender, ElapsedEventArgs e)
        {
            foreach (DeadlineUserData user in Users)
            {
                if (user.DeadlineType != DeadlineEnum.Repeater)
                    continue;

                int daysLeft = TotalDaysLeft(user.Deadline);
                if (user.DaysLeft == daysLeft)
                    continue;

                if (daysLeft > 0)
                {
                    user.DaysLeft = daysLeft;
                    FileSystem.UpdateFile(user);
                    continue;
                }
                
                SendMessage(user);
                
                user.Deadline = user.Deadline.AddDays(7);
                user.DaysLeft = TotalDaysLeft(user.Deadline);
                FileSystem.UpdateFile(user);
            }
        }
    }
}
