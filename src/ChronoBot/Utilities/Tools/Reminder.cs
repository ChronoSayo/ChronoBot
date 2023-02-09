using System;
using System.Collections.Generic;
using System.Threading;
using ChronoBot.Common.Systems;
using ChronoBot.Common.UserDatas;
using ChronoBot.Enums;
using ChronoBot.Helpers;
using Discord;
using Microsoft.Extensions.Configuration;

namespace ChronoBot.Utilities.Tools
{
    public class Reminder
    {
        private readonly IConfiguration _config;
        private readonly ReminderFileSystem _fileSystem;
        private Timer _timer;
        private readonly List<ReminderUserData> _users;

        public Reminder(IConfiguration config, ReminderFileSystem fileSystem)
        {
            _config = config;
            _fileSystem = fileSystem;
            _users = (List<ReminderUserData>)_fileSystem.Load();
        }

        public void SetReminder(string message, DateTime dateTime, string options)
        {

        }
    }
}
