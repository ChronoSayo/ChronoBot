using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Xml.Linq;
using TwitchLib.Api.Helix.Models.Users;

namespace ChronoBot.Interfaces
{
    interface IFileSystem
    {
        string PathToSaveFile { get; }
        string Category { get; set; }
        void Save(IUserData userData);
        List<IUserData> Load();
        List<IUserData> CollectUserData(Dictionary<XDocument, ulong> xmls, string category);
        void UpdateFile(IUserData userData);
        void DeleteInFile(IUserData userData);
    }
}
