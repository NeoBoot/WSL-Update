using Microsoft.Win32.TaskScheduler;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace WSL_Update
{
    static class Program
    {
        const string ProcName = "wsl";
        const string Parms =
            "sudo apt update && " +
            "sudo apt dist-upgrade -y && " +
            "sudo apt autoremove -y && " +
            "sudo do-release-upgrade";
        const string HelpMsg =
            "sudo visudo\n" +
            "\n" +
            "#user\n" +
            "user ALL=NOPASSWD: /usr/bin/apt,/usr/bin/do-release-upgrade";
        readonly static string Caption = Assembly.GetEntryAssembly()!.GetName().Name!;
        static readonly Process Runner = new()
        {
            StartInfo = new(ProcName, Parms)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            },
            EnableRaisingEvents = true,
        };
        static readonly Timer Timer = new() { Interval = 13 * 1000 };

        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (Process.GetProcessesByName(ProcName).Any())
                return;
            Runner.OutputDataReceived += OutputDataReceived;
            Timer.Tick += Tick;

            Runner.Start();
            Runner.BeginOutputReadLine();
            Timer.Start();
            Application.Run();
        }

        private static void Tick(object? sender, EventArgs e)
        {
            Runner.OutputDataReceived -= OutputDataReceived;
            Timer.Stop();
            Runner.Kill();
            MessageBox.Show(HelpMsg, Caption);
            Application.Exit();
        }

        static void Exited(object? sender, EventArgs e)
        {
#if RELEASE
            TaskService Service = new();
            var Definition = Service.NewTask();
            Definition.Triggers.Add(new WeeklyTrigger());
            Definition.Actions.Add(Path.ChangeExtension(Environment.GetCommandLineArgs()[0], "exe"));
            Definition.Settings.StartWhenAvailable = true;
            Service.RootFolder.RegisterTaskDefinition(Caption, Definition);
#endif
            Application.Exit();
        }

        static void OutputDataReceived(object? sender, DataReceivedEventArgs e)
        {
            Timer.Stop();
            Runner.Exited += Exited;
            Runner.OutputDataReceived -= OutputDataReceived;
        }
    }
}
