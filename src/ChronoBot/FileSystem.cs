
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChronoBot
{
    class FileSystem
    {
        private DiscordSocketClient _client;
        private string _path;
        private string _fileName;

        public FileSystem(string fileName, DiscordSocketClient client)
        {
            _fileName = fileName + ".txt";
            _path = System.Reflection.Assembly.GetEntryAssembly().Location;
        }

        public bool CheckFileExists()
        {
            if (File.Exists(Path.Combine(_path, _fileName)))
                return true;
            return false;
        }

        public void Save(string line)
        {
            using (StreamWriter writer = new StreamWriter(_fileName, true))
                writer.WriteLine(line);

            Console.WriteLine("SAVED " + line + " INTO " + _fileName + " BY " + _client.CurrentUser.Username);
        }

        public void Overwrite(string line)
        {
            File.WriteAllText(_fileName, string.Empty);
            using (StreamWriter writer = new StreamWriter(_fileName, true))
                writer.WriteLine(line);

            Console.WriteLine("OVERWRITE WITH " + line + " INTO " + _fileName);
        }

        public List<string> Load()
        {
            List<string> lines = new List<string>();

            StreamReader reader = new StreamReader(_fileName);
            while (!reader.EndOfStream)
                lines.Add(reader.ReadLine());
            reader.Close();

            return lines;
        }

        public bool CheckFound(string find)
        {
            bool found = false;
            List<string> lines = new List<string>();
            StreamReader reader = new StreamReader(_fileName);

            while (!reader.EndOfStream)
                lines.Add(reader.ReadLine());

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(find))
                {
                    found = true;
                    break;
                }
            }
            reader.Close();

            return found;
        }

        public string FindLine(string find)
        {
            string found = null;
            List<string> lines = new List<string>();
            StreamReader reader = new StreamReader(_fileName);

            while (!reader.EndOfStream)
                lines.Add(reader.ReadLine());

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains(find))
                {
                    found = lines[i];
                    break;
                }
            }
            reader.Close();

            return found;
        }

        public void UpdateFile(string find, string newText)
        {
            string[] lines = File.ReadAllLines(_fileName);
            int foundLine = 0;
            for (int j = 0; j < lines.Length; j++)
            {
                if (lines[j].Contains(find))
                {
                    foundLine = j;
                    break;
                }
            }

            lines[foundLine] = newText;
            File.WriteAllLines(_fileName, lines);
        }

        private void RemoveWhiteSpace()
        {
            string temp = Path.GetFileName("x" + _fileName);
            using (StreamReader reader = new StreamReader(_fileName))
            using (StreamWriter writer = new StreamWriter(temp))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        writer.WriteLine(line);
                }
            }

            File.Copy(temp, _fileName, true);
            File.Delete(temp);
        }

        /// <summary>
        /// Used when the file has an ID system.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="prefix"></param>
        public void FindAndDeleteByID(int ID)
        {
            string[] lines = File.ReadAllLines(_fileName);
            int foundLine = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(Info.ID_PREFIX + ID))
                {
                    foundLine = i;
                    break;
                }
            }

            string delLine = lines[foundLine];

            lines[foundLine] = "";
            File.WriteAllLines(_fileName, lines);

            RemoveWhiteSpace();

            Console.WriteLine("DELETED " + delLine + " FROM " + _fileName);
        }

        public bool DeleteLine(string del)
        {
            bool removed = false;

            string[] lines = File.ReadAllLines(_fileName);
            int foundLine = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == del)
                {
                    foundLine = i;
                    removed = true;
                    break;
                }
            }

            lines[foundLine] = "";
            File.WriteAllLines(_fileName, lines);

            RemoveWhiteSpace();

            return removed;
        }

        public void Clear()
        {
            File.WriteAllText(_fileName, string.Empty);
        }

        public void DeleteFile()
        {
            if (CheckFileExists())
                File.Delete(_fileName);
        }
    }
}
