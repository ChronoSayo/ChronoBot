using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace ChronoBot
{
    /// <summary>
    /// [0]!remind 
    /// [1]Insert remind message here
    /// [2]16/2 8:29
    /// [3]7
    /// 0: command, 1: message, 2: time (optional), 3: repeat (optional, only if there is time)
    /// </summary>
    class Remind
    {
        private DiscordSocketClient _client;
        private Timer _checkInterval;
        private FileSystem _fileSystem;
        private int _ID;
        private List<Reminders> _reminders;

        private const string _ADD_COMMAND = Info.COMMAND_PREFIX + "remindme";
        private const string _LIST_COMMAND = Info.COMMAND_PREFIX + "remindlist";
        private const string _DELETE_COMMAND = Info.COMMAND_PREFIX + "reminddelete";
        private const string _HOW_TO_COMMAND = Info.COMMAND_PREFIX + "remind";
        private const string _HOW_TO = "__HOW TO USE REMIND COMMAND:__ \n" +
            "***" + _ADD_COMMAND + "***\n" +
            "Message here. (Optional).\n" +
            "Time here ([hh:mm] [dd/mm]). If no time, remind is activated immediately. (Optional).\n" +
            "Repeat every [x] day(s) here. If no repeat, remind is once. (Optional).\n\n" +
            "***" + _LIST_COMMAND + "*** shows list of your reminders.\n" +
            "***" + _DELETE_COMMAND + "*** [id] deletes a reminder. " +
                "[id] is the ID number displayed from ***" + _LIST_COMMAND + "***";

        struct Reminders
        {
            public ulong userID;
            public DateTime time;
            public string message;
            public bool repeat;
            public int repeatDays;
            public int ID;
        }

        public Remind(DiscordSocketClient client)
        {
            _client = client;

            _reminders = new List<Reminders>();

            _ID = 0;

            CreateFile();
            CheckInterval();
        }

        private void CreateFile()
        {
            _fileSystem = new FileSystem("remind", _client);

            if (_fileSystem.CheckFileExists())
            {
                List<string> newList = _fileSystem.Load();
                if (newList.Count > 0)
                {
                    foreach (string s in newList)
                        FormatReminders(s);
                }
            }
        }

        private void HowTo(SocketMessage socketMessage)
        {
            string msg = socketMessage.ToString();
            if (msg.ToLower() == _HOW_TO_COMMAND.ToLower())
                Info.SendMessageToChannel(socketMessage, _HOW_TO);
        }

        private void ListReminders(SocketMessage socketMessage)
        {
            string msg = socketMessage.ToString();
            if (msg.ToLower() == _LIST_COMMAND.ToLower())
            {
                string list = string.Empty;
                for (int i = 0; i < _reminders.Count; i++)
                {
                    //If in debug, post whole list or else the user's guild.
                    bool addToList = false;
                    if (Info.DEBUG)
                        addToList = true;
                    else
                        addToList = _reminders[i].userID == socketMessage.Author.Id;
                    if (addToList)
                    {
                        list += Info.BULLET_LIST + " " + _reminders[i].message + "\n*" +
                            _client.GetUser(_reminders[i].userID).Username + "*\n" +
                            _reminders[i].time + " **ID: " + _reminders[i].ID + "**\n";
                    }
                }
                if (string.IsNullOrEmpty(list))
                    Info.Shrug(socketMessage);
                else
                    Info.SendMessageToChannel(socketMessage, list);
            }
        }

        private void DeleteReminder(SocketMessage socketMessage)
        {
            string message = socketMessage.ToString();
            string[] split = message.Split(' ');
            string command = split[0];
            if (command.ToLower() == _DELETE_COMMAND)
            {
                bool success = split.Length == 2;
                if (success)
                {
                    string idIn = split[1];
                    int i = -1;
                    success = int.TryParse(idIn, out i);
                    if (success)
                    {
                        success = i >= 0;
                        if (success)
                        {
                            int idOut = GetIndexByID(i);
                            success = idOut >= 0;
                            if (success)
                            {
                                EraseReminder(_reminders[idOut]);
                                Info.SendMessageToChannel(socketMessage, "Done.");
                            }
                        }
                    }
                }
                if (!success)
                    Info.Shrug(socketMessage);
            }
        }

        private DateTime GetTime(string message)
        {
            string[] split = message.Split(' ');
            DateTime date = DateTime.Now;
            if (!DateTime.TryParse(split[0], out date))
            {
                DateTime time = DateTime.Now;
                if (split.Length > 1 && !DateTime.TryParse(split[1], out time))
                {
                    if (split.Length > 1)
                    {
                        date = date.AddHours(time.Hour);
                        date = date.AddMinutes(time.Minute);
                        date = date.AddSeconds(time.Second);
                    }
                }
            }
            return date;
        }

        private bool GetRepeat(string message, out int days)
        {
            days = int.MinValue;
            bool repeat = true;
            if (message == string.Empty || !int.TryParse(message, out days))
                repeat = false;
            return repeat;
        }

        private void Save(Reminders newReminder)
        {
            string msg = newReminder.message;
            string time = newReminder.time.ToString();
            ulong userID = newReminder.userID;
            _ID++;
            int id = newReminder.ID = _ID;
            int repeat = newReminder.repeat ? 1 : 0;
            int repeatDays = newReminder.repeatDays;

            string save = msg + "|" + time + "|" + userID + "|%" + id + "|" + repeat + "|" + repeatDays;
            _fileSystem.Save(save);

            _reminders.Add(newReminder);
        }

        private void Update(Reminders newReminder, int i)
        {
            _reminders[i] = newReminder;
            string msg = newReminder.message;
            string time = newReminder.time.ToString();
            ulong userID = newReminder.userID;
            int id = newReminder.ID;
            int repeat = newReminder.repeat ? 1 : 0;
            int repeatDays = newReminder.repeatDays;

            string updateText = msg + "|" + time + "|" + userID + "|%" + id + "|" + repeat + "|" + repeatDays;
            _fileSystem.UpdateFile(Info.ID_PREFIX + id, updateText);
        }

        //Format text from file.
        private void FormatReminders(string line)
        {
            string[] split = line.Split('|');
            string msg = split[0];
            string time = split[1];
            string userID = split[2];
            string id = split[3].Substring(1);
            string repeatText = split[4];
            string repeatDays = split[5];
            List<ulong> getRemindUsersIDs = new List<ulong>();

            if (split.Length > 6)
            {
                string[] listSplit = split[6].Split(Info.ID_PREFIX.ToCharArray());
                foreach (string s in listSplit)
                    getRemindUsersIDs.Add(ulong.Parse(s));
            }

            Reminders remind = new Reminders
            {
                message = msg,
                time = DateTime.Parse(time),
                userID = ulong.Parse(userID),
                ID = int.Parse(id),
                repeat = repeatText == "0" ? false : true,
                repeatDays = int.Parse(repeatDays)
            };

            _reminders.Add(remind);

            _ID = remind.ID;
        }

        private void CheckInterval()
        {
            int interval = Info.DEBUG ? 1 : 10;
            _checkInterval = new Timer(interval * 1000)//10 seconds.
            {
                Enabled = true,
                AutoReset = true
            };
            _checkInterval.Elapsed += _checkInterval_Elapsed;
        }

        private void _checkInterval_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_reminders.Count == 0)
                return;

            List<Reminders> deleteList = new List<Reminders>();
            for (int i = 0; i < _reminders.Count; i++)
            {
                Reminders r = _reminders[i];
                if (r.time <= DateTime.Now)
                {
                    SocketUser user = _client.GetUser(r.userID);
                    string message = "***REMINDER*** \n" + r.message;
                    //Failed to give remind will push a message on how to do it.
                    if (r.message == string.Empty)
                        message += _HOW_TO;
                    Info.SendMessageToUser(user, message);
                    if (r.repeat)
                    {
                        r.time = r.time.AddDays(r.repeatDays);
                        Update(r, i);
                    }
                    else
                        deleteList.Add(r);
                }
            }

            for (int i = 0; i < deleteList.Count; i++)
            {
                EraseReminder(deleteList[i]);
            }
        }

        private void EraseReminder(Reminders r)
        {
            _fileSystem.FindAndDeleteByID(r.ID);
            _reminders.Remove(r);
        }

        public void MessageReceived(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot)
                return;
            HandleRemind(socketMessage);
            DeleteReminder(socketMessage);
            ListReminders(socketMessage);
            HowTo(socketMessage);
        }

        private void HandleRemind(SocketMessage socketMessage)
        {
            string sm = socketMessage.ToString();
            if (!sm.Contains(_ADD_COMMAND))
                return;
            Reminders newReminder = new Reminders();
            //0: command, 1: message, 2: time (optional), 3: repeat (optional, only if there is time)
            string[] lines = sm.Split('\n');

            int i = 1;
            //Store message
            if (lines.Length > i)
                newReminder.message = lines[i];
            else
                newReminder.message = string.Empty;

            //Store time
            i++;
            if (lines.Length > i)
                newReminder.time = GetTime(lines[i]);
            else
                newReminder.time = DateTime.Now;

            //Store repeat and days
            i++;
            if (newReminder.time != DateTime.Now && lines.Length > i)
            {
                int days = int.MinValue;
                newReminder.repeat = GetRepeat(lines[i], out days);
                if (newReminder.repeat)
                    newReminder.repeatDays = days;
            }
            else
                newReminder.repeat = false;

            //Store user ID
            newReminder.userID = socketMessage.Author.Id;

            //Save
            Save(newReminder);

            //Confirm
            string confirmedMessage = "Reminding " + socketMessage.Author.Mention + " at " + newReminder.time + ".";
            Info.SendMessageToChannel(socketMessage, confirmedMessage);
        }

        private int GetIndexByID(int id)
        {
            int index = -1;
            for (int i = 0; i < _reminders.Count; i++)
            {
                if (id == _reminders[i].ID)
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
    }
}
