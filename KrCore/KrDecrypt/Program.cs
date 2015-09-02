
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using SevenZip;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Dreamware
{ 
    public class Decrypt
    { 
        public event Action<object, string, string> Matched;

        public bool IsMatch { get; set; }

        private string Path = null;

        private List<string> PswList = null;

        private List<string> logStr = new List<string>();

        public Decrypt(string path,List<string> pswList)
            :this()
        {
            Path = path;
            PswList = pswList;
        } 

        private Decrypt()
        {
            IsMatch = false;
        }
         
        string GetDir()
        {
            FileInfo tempInfo = new FileInfo(Path);
            return tempInfo.DirectoryName;
        }

        public void Run()
        {
            if (Path == null || PswList == null)
                return; 

            SevenZipExtractor.SetLibraryPath(@"c:\Program Files\7-Zip\7z.dll");
 
            FileInfo info = new FileInfo(Path);
 
            string fileName = info.Name;
            string dirName = info.DirectoryName;

            object param = new Tuple<List<string>,string>( PswList, dirName);
            Guess(param); 
        } 

        private void Guess(object param)
        {
            var p = (Tuple<List<string>, string>)param;

            List<string> pswdGroup = p.Item1;
            string dirName = p.Item2;
             
            foreach (var password in pswdGroup)
            {
                if (IsMatch)
                    break;

                using (var zip = new SevenZipExtractor(Path, password))
                {
                    zip.FileExtractionStarted += (s, e) =>
                    {
                        Console.WriteLine("match:" + password);
                        logStr.Add("match:" + password);

                        using (StreamWriter sw = new StreamWriter("psw.txt", true, Encoding.UTF8))
                        {
                            sw.WriteLine(Path + "=>" + password);
                            Console.WriteLine("saved:" + "psw.txt");
                        }

                        IsMatch = true;

                        if (Matched != null)
                            Matched(this, Path, password);

                        e.Cancel = true;

                        Console.WriteLine("#matched");
                    };

                    zip.FileExists += (o, e) =>
                    {
                        Console.WriteLine("match:" + password);
                        logStr.Add("match:" + password);
                        
                        using (StreamWriter sw = new StreamWriter("psw.txt",true,Encoding.UTF8))
                        {
                            sw.WriteLine(Path + "=>"+password); 
                            Console.WriteLine("saved:"+ "psw.txt");
                        }

                        IsMatch = true;

                        if (Matched != null)
                            Matched(this, Path, password);

                        //Console.WriteLine("Warning: file \"" + e.FileName + "\" already exists.");
                        e.Cancel = true;

                        Console.WriteLine("#matched");
                    };
                    zip.ExtractionFinished += (s, e) => 
                    {
                        IsMatch = true;
                    };

                    try
                    {
                        zip.ExtractArchive(dirName);
                    }
                    catch (SevenZipArchiveException ex)
                    {
                        Console.WriteLine("mismatch:" + password);
                        logStr.Add("mismatch:" + password);
                        continue;
                    }
                }
            } 
        }
    } 

    class Program
    {  
        static string basePswA = "陈志武韩文字幕http://chieziv.ys168.com/禁止转载";
        static string basePswB = "陈志武韩文字幕chieziv.ys168.com禁转";
 
        protected static List<string> PrepareForStd(int LowerBound,int UpperBound)
        {
            List<string> PswList = new List<string>();
            for (int i = LowerBound; i <= UpperBound; ++i)
            {
                PswList.Add(basePswA + i.ToString());
                if (i < 10)
                    PswList.Add(basePswA + i.ToString("00"));
                if (i < 100)
                    PswList.Add(basePswA + i.ToString("000"));

                PswList.Add(basePswB + i.ToString());
                if (i < 10)
                    PswList.Add(basePswB + i.ToString("00"));
                if (i < 100)
                    PswList.Add(basePswB + i.ToString("000"));
            }

            return PswList;
        }

        protected static List<string> PrepareForExt(int LowerBound, int UpperBound)
        {
            List<string> PswListABC = new List<string>();

            List<char> sand = new List<char>();
            for (int i = 0; i <= 9; ++i)
                sand.Add((char)(i + '0'));

            for (char i = 'a'; i <= 'z'; ++i)
                sand.Add(i);
 
            for (char i = (char)LowerBound; i <= (char)UpperBound; ++i)
            { 
                foreach(var chA in sand)
                {
                    foreach (var chB in sand)
                    {
                        string pattern = string.Format("{0}{1}{2}", i, chA, chB);

                        if (PswListABC.Contains(pattern))
                            continue;

                        PswListABC.Add(basePswA + pattern); 
                    }
                } 
            }

            return PswListABC;
        }

        private static void Inst_Matched(object arg1, string arg2, string arg3)
        {
             foreach(var t in threads)
             {
                 if (t.IsAlive)
                     t.Abort();
             }
        }

        static void RunCmd(string cmd)
        {
            using (Process process = new Process())
            {
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.Arguments = @"C:\Windows\System32\cmd.exe";
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.Verb = "RunAs";

                process.Start();
                process.ErrorDataReceived += (o, e) =>
                {
                    Console.WriteLine(e.Data);
                };

                process.OutputDataReceived += (o, e) =>
                {
                    Console.WriteLine(e.Data);
                };

                process.StandardInput.WriteLine(cmd + "&exit");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }

        public static void GoStd(string path)
        {
            for (int i = 0; i < 10; ++i)
            {
                int LowerBound = i * 100;
                int UpperBound = (i + 1) * 100 - 1;

                Thread t = new Thread(() =>
                { 
                    Decrypt inst = new Decrypt(path, PrepareForStd(LowerBound, UpperBound));
                    inst.Matched += Inst_Matched;
                    inst.Run();
                });

                t.Priority = ThreadPriority.Highest;

                threads.Add(t);

                t.Start();

                Console.WriteLine(string.Format("Process for RANGE({0} - {1}) started", LowerBound, UpperBound));
            }
        }

        public static void GoExt(string path)
        {
            for (char ch = 'a'; ch <= 'z'; ch++)
            {
                Thread t = new Thread(() =>
                {
                    Decrypt inst = new Decrypt(path, PrepareForExt(ch, ch));
                    inst.Matched += Inst_Matched;
                    inst.Run();
                });

                t.Priority = ThreadPriority.Highest;

                threads.Add(t);

                t.Start();

                Console.WriteLine(string.Format("Process for RANGE({0} - {1}) started", ch, ch));
            }
        }

        public static List<Thread> threads = new List<Thread>();

        static void Main(string[] args)
        {
            foreach(var arg in args) 
                Console.Write(arg+" ");
            Console.WriteLine();

            if (args.Length  == 4)
            {
                switch(args[0].ToLower())
                {
                    case "std":
                        {
                            int LowerBound = int.Parse(args[2]);
                            int UpperBound = int.Parse(args[3]);
 
                            Decrypt inst = new Decrypt(args[1], PrepareForStd(LowerBound,UpperBound));
                            inst.Matched += Inst_Matched;
                            inst.Run();
                        }
                        break;
                    case "ext":
                        {
                            int LowerBound = (int)char.Parse(args[2]);
                            int UpperBound = (int)char.Parse(args[3]); 

                            Decrypt inst = new Decrypt(args[1], PrepareForExt(LowerBound,UpperBound));
                            inst.Matched += Inst_Matched;
                            inst.Run();
                        }
                        break;
                }
                
            }
            else if(args.Length == 3)
            {
                if (args[0] == "dict")
                {
                    string path = args[1];
                    string dicfile = args[2];

                    try
                    {
                        using (StreamReader sr = new StreamReader(dicfile))
                        {
                            List<string> pswList = new List<string>();
                            for (; !sr.EndOfStream; )
                            {
                                pswList.Add(sr.ReadLine());
                            }

                            Decrypt inst = new Decrypt(path, pswList);
                            inst.Matched += Inst_Matched;
                            inst.Run();
                        }
                    }
                    catch (Exception ex)
                    {

                    } 
                }
            }
            else
            {
                string path = string.Empty;

                for (; ; )
                {
                    Console.Write("path:");
                    path = Console.ReadLine();

                    if (path == "" || path.Trim() == "")
                        continue;

                    if (!path.Contains(".7z"))
                        continue;

                    if (path.Contains(".7z."))
                    {
                        FileInfo tempInfo = new FileInfo(path);

                        var newPath = path.Replace(tempInfo.Extension, "");

                        if (!File.Exists(newPath))
                        {
                            string cmdStr = string.Format("cd {0}\ncopy /y /b \"{1}.00*\" \"{2}\"",
                                tempInfo.DirectoryName,
                                newPath,
                                newPath);

                            RunCmd(cmdStr);
                        }

                        path = newPath;
                    }

                    break;
                }

                for (; ; )
                {
                    Console.Write("std(1) or ext(0):");
                    string read = Console.ReadLine();

                    if (read == "1")
                    {
                        GoStd(path);
                        break;
                    }
                    else if (read == "0")
                    {
                        GoExt(path);
                        break;
                    }
                }

                for(;threads.Any(m=>m.IsAlive);)
                { 
                }
            }

            Console.WriteLine("#finished"); 

            return; 
        } 
    }    
}
