using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ChronoBot.Enums;

namespace ChronoBot.Interfaces
{
    interface IFileSystem
    {
        string PathToSaveFile { get; }
        bool Save(IUserData userData);
        IEnumerable<IUserData> Load();
        bool UpdateFile(IUserData userData);
        bool DeleteInFile(IUserData userData);
    }

    public abstract class FileSystem : IFileSystem
    {
        public abstract string PathToSaveFile { get; }
        public abstract bool Save(IUserData userData);

        public abstract IEnumerable<IUserData> Load();

        public abstract bool UpdateFile(IUserData userData);

        public abstract bool DeleteInFile(IUserData userData);
    }
}
