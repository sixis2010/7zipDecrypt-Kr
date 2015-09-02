using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dreamware
{   
    public class KrBat
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

         List<Process> runningProcess = new List<Process>();
         List<Process> pendingProcess = new List<Process>();

         List<Process> backupProcess = new List<Process>();

         Timer tmr = null;

         string path = null;

         private bool IsMatched = false;

         private int threadCnt = 7;
 
         public KrBat(string _path)
             :this()
         {
             path = _path;
         }

         ~KrBat()
         {
             Cleanup();  
         }

         public void Cleanup()
         {
             if (tmr != null)
             {
                 tmr.Dispose();
                 tmr = null;
             } 

             foreach (var p in GetAllProcess())
                 Remove(p); 

            try
            {
                foreach (var p in GetAllProcess())
                {
                    ExitProcess(p);
                }
            }
            catch (Exception ex)
            { }
         }

         bool ExitProcess(Process p)
         {
             return TerminateProcess(p.Handle,0);
         }

         protected KrBat()
         {
             tmr = new Timer((o) => {
                 if (pendingProcess.Count() == 0)
                     return;

                 for (; runningProcess.Count() < threadCnt && pendingProcess.Count()>0; )
                 {
                     var next = pendingProcess.FirstOrDefault();

                     pendingProcess.Remove(next);

                     runningProcess.Add(next);

                     next.Start();

                     next.BeginOutputReadLine();
                     next.BeginErrorReadLine();
                 }
             },
             null,
             0,
             1000);
         }

         private void Add(Process p)
         {
             if (pendingProcess == null)
                 pendingProcess = new List<Process>();

             if (!pendingProcess.Any(m => m == p))
             {
                 pendingProcess.Add(p);
                 backupProcess.Add(p);
             }
         }

         private void Remove(Process p)
         {
             if (pendingProcess == null)
                 pendingProcess = new List<Process>();

             pendingProcess.Remove(p);
             runningProcess.Remove(p);
             try
             {
                 ExitProcess(p);
             }
             catch(Exception ex)
             {

             } 
         }

         public List<Process> GetAllProcess()
         {
             var tempList = new List<Process>();
             tempList.AddRange(pendingProcess);
             tempList.AddRange(runningProcess);
             return tempList;
         }

         private Process NewTask(string arguments)
         {
             Process process = new Process();
             process.StartInfo.CreateNoWindow = false;
             process.StartInfo.FileName = "KrDecrypt.exe";
             process.StartInfo.UseShellExecute = false;
             process.StartInfo.Arguments = arguments; 
             process.StartInfo.RedirectStandardError = true;
             process.StartInfo.RedirectStandardOutput = true;
             process.StartInfo.RedirectStandardError = true;
             process.Exited += (o,e) =>
             {
                 Remove(o as Process);
             };
             process.OutputDataReceived += (o, e) =>
             {
                 if (IsMatched)
                     return;

                 if (e.Data == "#matched")
                 {
                     IsMatched = true;

                     Cleanup();

                     return;
                 }

                 Console.WriteLine(e.Data);
             };

             process.ErrorDataReceived += (o, e) =>
             {
                 if (IsMatched)
                     return;

                 Console.WriteLine(e.Data);
             };

             process.StartInfo.Verb = "RunAs";

             process.EnableRaisingEvents = true;
  
             return process;
         }

         private Process NewTask(object arg0, object arg1, object arg2, object arg3)
         {
             var param = string.Format("{0} {1} {2} {3}", arg0, arg1, arg2, arg3);
             return NewTask(param);
         }

         private Process NewTask(object arg0, object arg1, object arg2)
         {
             var param = string.Format("{0} {1} {2}", arg0, arg1, arg2);
             return NewTask(param);
         }

        public void GoStd()
        {
            for (int i = 0; i < 10; ++i)
            {
                int start = i * 100;
                int end = (i + 1) * 100 - 1;

                var proc = NewTask("std", path, start, end);
                Add(proc);

                Console.WriteLine(string.Format("Process for RANGE({0} - {1}) added", start, end));
            }
        }

        public void GoExt()
        {
            for (char ch = 'a'; ch <= 'z'; ch++)
            {
                var proc = NewTask("ext", path, ch, ch);
                Add(proc);

                Console.WriteLine(string.Format("Process for RANGE({0} - {1}) added", ch, ch));
            }
        }

        public void GoDict(string dicfile)
        {
            List<string> pswList = new List<string>();
            using(StreamReader sr = new StreamReader(dicfile))
            {
                for(;!sr.EndOfStream;)
                {
                    pswList.Add(sr.ReadLine());
                }
            }

            var PswGroup = new List<List<string>>();
            int j = pswList.Count() / threadCnt;
            for (int i = 0; i < pswList.Count; i += pswList.Count() / threadCnt)
            {
                List<string> cList = new List<string>();
                cList = pswList.Take(j).Skip(i).ToList();
                j += pswList.Count() / threadCnt;
                PswGroup.Add(cList);
            }

            foreach(var gp in PswGroup)
            { 
                string newDicfileName = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+"\\"+ Guid.NewGuid().ToString("N").ToUpper();

                StreamWriter sw = new StreamWriter(newDicfileName);
                foreach (var ln in gp)
                    sw.WriteLine(ln);
                sw.Close();

                var proc = NewTask("dict", path, newDicfileName);
                Add(proc);
            }
        }

        public bool CanExit()
        {
            var procList = GetAllProcess();

            return procList.Count()==0;
        } 

        public bool Matched()
        {
            return IsMatched;
        }
    }

    class Program
    { 
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

        static void Main(string[] args)
        {
            string path = string.Empty;

            for (; ;)
            {
                Console.WriteLine("KrBAT is a bruteforce PASSWORD crack program for 7zip. Use at your own risk.\n(c)2015 Dreamware(R) Inc. \nWritten by Shijie. All rights reserved.");


                Console.WriteLine("\n\n===================================================================================");
                Console.WriteLine("Input 7z file path(choose any single file for volume archives end up with .7z.00*):");
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

            KrBat inst = new KrBat(path); 

            for (; ; )
            {
                Console.Write("ext(0),std(1) or dic(2):");
                string read = Console.ReadLine();
                 
                if (read == "1")
                {
                    inst.GoStd();
                    break;
                }
                else if (read == "0")
                {
                    inst.GoExt();
                    break;
                }
                else if(read == "2")
                {    
                    Console.Write("Input password dictionary file path\n");
                    Console.Write("path:");

                    string dicfile = Console.ReadLine();

                    inst.GoDict(dicfile);

                    break;
                }
            }

            for (; !inst.Matched(); )
            {
                if(!inst.CanExit())
                {
                    Thread.Sleep(1000); 
                }
                else 
                { 
                    break;
                }
            }
 
            Console.WriteLine(inst.Matched()?"A match was found":"No match was found");

            Console.WriteLine("Press any key to exit..."); 

            Console.ReadKey();

            return;
        } 
    }
}
