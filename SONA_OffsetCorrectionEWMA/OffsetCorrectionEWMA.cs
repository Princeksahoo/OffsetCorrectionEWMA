using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SONA_OffsetCorrectionEWMA
{
    public partial class OffsetCorrectionEWMA : ServiceBase
    {
        List<Thread> threads = new List<Thread>();
        List<CreateClient> clients = new List<CreateClient>();
        string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private volatile bool stopping = false;
        public OffsetCorrectionEWMA()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Thread.CurrentThread.Name = "SONA_OffsetcorrectionEWMA";

            if (!Directory.Exists(appPath + "\\Logs\\"))
            {
                Directory.CreateDirectory(appPath + "\\Logs\\");
            }

            List<MachineInfoDTO> machines = DatabaseAccess.GetTPMTrakMachine();
            if (machines.Count == 0)
            {
                Logger.WriteDebugLog("No machine is enabled for TPM-Trak. modify the machine setting and restart the service.");
                return;
            }

            try
            {
                foreach (MachineInfoDTO machine in machines)
                {
                    //MachineInfoDTO machine = machines[0]; //g: test
                    CreateClient client = new CreateClient(machine);
                    clients.Add(client);

                    ThreadStart job = new ThreadStart(client.GetClient);
                    Thread thread = new Thread(job);
                    thread.Name = SafeFileName(machine.MachineId);
                    thread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                    thread.Start();
                    threads.Add(thread);
                    Logger.WriteDebugLog(string.Format("Machine {0} started for DataCollection", machine.MachineId));

                }
            }
            catch (Exception e)
            {
                Logger.WriteErrorLog(e.ToString());
            }
        }
        internal void StartDebug()
        {
            OnStart(null);
        }

        protected override void OnStop()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
            {
                Thread.CurrentThread.Name = "SONA_OffsetcorrectionEWMA";
            }

            stopping = true;
            ServiceStop.stop_service = 1;

            Thread.SpinWait(60000 * 10);
            try
            {
                Logger.WriteDebugLog("Service Stop request has come!!! ");
                Logger.WriteDebugLog("Thread count is: " + threads.Count.ToString());
                foreach (Thread thread in threads)
                {
                    if (thread != null && thread.IsAlive)
                    {
                        // Try to stop by allowing the thread to stop on its own.
                        this.RequestAdditionalTime(6000);
                        if (!thread.Join(6000))
                        {
                            thread.Abort();
                            Logger.WriteDebugLog("Aborted.");
                        }
                    }
                }
                threads.Clear();
            }
            catch (Exception ex)
            {
                Logger.WriteErrorLog(ex.ToString());
            }
            finally
            {
                clients.Clear();
            }
            Logger.WriteDebugLog("Service has stopped.");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = args.ExceptionObject as Exception;
            if (e != null)
            {
                Logger.WriteErrorLog("Unhandled Exception caught : " + e.ToString());
                Logger.WriteErrorLog("Runtime terminating:" + args.IsTerminating);
                var threadName = Thread.CurrentThread.Name;
                Logger.WriteErrorLog("Exception from Thread = " + threadName);
                Process p = Process.GetCurrentProcess();
                StringBuilder str = new StringBuilder();
                if (p != null)
                {
                    str.AppendLine("Total Handle count = " + p.HandleCount);
                    str.AppendLine("Total Threads count = " + p.Threads.Count);
                    str.AppendLine("Total Physical memory usage: " + p.WorkingSet64);

                    str.AppendLine("Peak physical memory usage of the process: " + p.PeakWorkingSet64);
                    str.AppendLine("Peak paged memory usage of the process: " + p.PeakPagedMemorySize64);
                    str.AppendLine("Peak virtual memory usage of the process: " + p.PeakVirtualMemorySize64);
                    Logger.WriteErrorLog(str.ToString());
                }
                Thread.CurrentThread.Abort();
            }
        }

        public string SafeFileName(string name)
        {
            StringBuilder str = new StringBuilder(name);
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                str = str.Replace(c, '_');
            }
            return str.ToString();
        }
    }
}
