using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChronoBot.Interfaces
{
    interface IFileSystem
    {
        string PathToSaveFile { get; }
        string Category { get; set; }
        void Save(IUserData userData);
        IEnumerable<IUserData> Load();
        IEnumerable<IUserData> CollectUserData(Dictionary<XDocument, ulong> xmls, string category);
        void UpdateFile(IUserData userData);
        void DeleteInFile(IUserData userData);
    }
}
