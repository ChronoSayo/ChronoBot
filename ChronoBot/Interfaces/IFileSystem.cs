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
        Task SaveAsync(IUserData userData);
        Task<IEnumerable<IUserData>> LoadAsync();
        IEnumerable<IUserData> CollectUserDataAsync(Dictionary<XDocument, ulong> xmls, string category);
        Task UpdateFileAsync(IUserData userData);
        Task DeleteInFileAsync(IUserData userData);
        Stream GetPathInStream(string path);
    }
}
