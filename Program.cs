using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

/*Задание 2 (du для виндоус )Создать приложение для подсчета занимаемого места указанной
пользователем директории. В качестве аргументов программа должна принимать
макс. уровень вложенности, отображать ли в виде (human readable) и сортировать
или нет (аналог du в linux). Подсчет должен производится в отдельном потоке.
*/

namespace lab1
{
    class Program
    {
        public long FolderSize(string str)
        {
            long size = 0;
            DirectoryInfo dirInfo = new DirectoryInfo(str);
            DirectoryInfo[] folders = dirInfo.GetDirectories();
            FileInfo[] filesInfo = dirInfo.GetFiles();
            foreach (FileInfo file in filesInfo)
            {
                size += file.Length;
            }
            for (int i = 0; i < folders.Length; i++)
            {
                size += FolderSize(str + "\\" + folders[i].Name);
            }
            return size;
        }

        public Dictionary<string, long> GetDirsAndFiles(string dirName, int curLevel, int maxLevel)
        {
            if (Directory.Exists(dirName))
            {
                string[] dirs = Directory.GetDirectories(dirName);
                Dictionary<string, long> dirsInfo = new Dictionary<string, long>(dirs.Length);
                foreach (string s in dirs)
                {
                    dirsInfo.Add(s, FolderSize(s));
                    dirsInfo = dirsInfo.Union(GetFiles(s)).ToDictionary(k => k.Key, v => v.Value);
                    if (curLevel < maxLevel)
                    {
                        var dirsInfo2 = GetDirsAndFiles(s, curLevel + 1, maxLevel);
                        dirsInfo = dirsInfo.Union(dirsInfo2).ToDictionary(k => k.Key, v => v.Value);
                    }
                }
                return dirsInfo;
            }
            return new Dictionary<string, long>(0);
        }

        public Dictionary<string, long> GetFiles(string dirName)
        {
            if (Directory.Exists(dirName))
            {
                string[] files = Directory.GetFiles(dirName);
                Dictionary<string, long> filesInfo = new Dictionary<string, long>(files.Length);
                foreach (string s in files)
                {
                    filesInfo.Add(s, s.Length);
                }
                return filesInfo;
            }
            return new Dictionary<string, long>(0);
        }

        public string GetBytesReadable(long i)
        {
            long absolute_i = (i < 0 ? -i : i);
            string suffix;
            double readable;
            if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            readable = (readable / 1024);
            return readable.ToString("0.### ") + suffix;
        }

        public void Process(string[] args)
        {
            int maxLevel = 0;
            if(args.Length == 0)
            {
                Console.WriteLine("Запустите программу с ключом -help, чтобы узнать подробности");
                return;
            }
            else if (args.Length == 1)
            {
                if (args[0] == "-help")
                {
                    Console.WriteLine("DU для Windows");
                    Console.WriteLine("Пример использования: [полный путь до директории] [максимальный уровень вложенности] [команда|последовательность команд|ничего]");
                    Console.WriteLine("Поддерживаемые команды:");
                    Console.WriteLine("1: -hr - отображать в виде 'human readable'");
                    Console.WriteLine("2: -sort - отображать в отсортированном виде");
                    return;
                }
            }
            else if (args.Length == 2)
            {
                if (int.TryParse(args[1], out maxLevel))
                {
                    Console.WriteLine(@args[0] + " - " + FolderSize(@args[0]));
                    Dictionary<string, long> dirsInfo = GetDirsAndFiles(@args[0], 0, maxLevel);
                    foreach (KeyValuePair<string, long> KeyValue in dirsInfo)
                    {
                        Console.WriteLine(KeyValue.Value+ " - " + KeyValue.Key);
                    }
                    return;
                }
                Console.WriteLine("Запустите программу с ключом -help, чтобы узнать подробности");
            }
            else if (args.Length == 3 & int.TryParse(args[1], out maxLevel))
            {
                if (args[2] == "-hr")
                {
                    Console.WriteLine(@args[0] + " - " + GetBytesReadable(FolderSize(@args[0])));
                    Dictionary<string, long> dirsInfo = GetDirsAndFiles(@args[0], 0, maxLevel);
                    foreach (KeyValuePair<string, long> KeyValue in dirsInfo)
                    {
                        Console.WriteLine(GetBytesReadable(KeyValue.Value) + " - " + KeyValue.Key);
                    }
                    return;
                }
                else if (args[2] == "-sort")
                {
                    Console.WriteLine(@args[0] + " - " + FolderSize(@args[0]));
                    Dictionary<string, long> dirsInfo = GetDirsAndFiles(@args[0], 0, maxLevel);
                    var sortedDirsInfo = from entry in dirsInfo orderby entry.Value descending select entry;
                    foreach (KeyValuePair<string, long> KeyValue in sortedDirsInfo)
                    {
                        Console.WriteLine(KeyValue.Value + " - " + KeyValue.Key);
                    }
                    return;
                }
            }
            else if (args.Length == 4 & int.TryParse(args[1], out maxLevel))
            {
                if (args[2] == "-hr" & args[3] == "-sort" || args[2] == "-sort" & args[3] == "-hr")
                {
                    Console.WriteLine(@args[0] + " - " + GetBytesReadable(FolderSize(@args[0])));
                    Dictionary<string, long> dirsInfo = GetDirsAndFiles(@args[0], 0, maxLevel);
                    var sortedDirsInfo = from entry in dirsInfo orderby entry.Value descending select entry;
                    foreach (KeyValuePair<string, long> KeyValue in sortedDirsInfo)
                    {
                        Console.WriteLine(GetBytesReadable(KeyValue.Value) + " - " + KeyValue.Key);
                    }
                    return;
                }
            }
            Console.WriteLine("Запустите программу с ключом -help, чтобы узнать подробности");
        }
        static void Main(string[] args)
        {
            Program p = new Program();
            Thread myThread = new Thread(new ThreadStart(delegate() { p.Process(args); }));
            myThread.Start();
            while (myThread.IsAlive)
            {
                Console.WriteLine("Задержка началась");
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("Задержка закончилась");
        }
    }
}
