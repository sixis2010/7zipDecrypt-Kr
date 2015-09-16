using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KrDirWalker
{
    class Program
    {
        public static List<FileInfo> Walk(DirectoryInfo dir)
        {
            List<FileInfo> FileList = new List<FileInfo>();

            FileInfo[] allFile = dir.GetFiles();
            foreach (FileInfo fi in allFile)
            {
                FileList.Add(fi);
            }

            DirectoryInfo[] allDir = dir.GetDirectories();
            foreach (DirectoryInfo d in allDir)
            {
                FileList.AddRange(Walk(d));
            }
            return FileList;
        }


        static string GetNumIndex(string name)
        {
            return name.Substring(3, 3);
        }

        static void Main(string[] args)
        {
            string Path = @"D:\krdrama\KoreanDrama\高清";

            var allFiles = Walk(new DirectoryInfo(Path));

            Dictionary<int, string> dic = new Dictionary<int, string>();

            foreach(var f in allFiles)
            {
                var idx = GetNumIndex(f.Name);

                var iidx = int.Parse(idx);

                dic[iidx] = f.Name;

            }

            for (int i = 1; i <= 188;++i)
            {
                if(!dic.ContainsKey(i))
                {
                    Console.WriteLine(i);
                }
            }

                for (; ; )
                {
                    Console.Write("dir:");
                    string dir = Console.ReadLine();

                    var baseDir = new DirectoryInfo(dir);
                    var files = Walk(baseDir);

                    foreach (var file in files)
                    {
                        var tarpath = baseDir.FullName + "/" + file.Name;
                        if (tarpath == file.FullName)
                            continue;

                        if (!File.Exists(tarpath))
                            File.Move(file.FullName, tarpath);
                        else
                            File.Move(file.FullName, baseDir.FullName + "/" + "Z_" + file.Name.Replace(file.Extension, "") + Guid.NewGuid().ToString("N").ToUpper() + file.Extension);

                    }
                } 
        }
    }
}
