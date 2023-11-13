using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.Win32.TaskScheduler;

if (!string.IsNullOrEmpty(Run($"/bin/sh -c \"echo '{Run("whoami")} ALL=NOPASSWD:/usr/bin/apt,/usr/bin/do-release-upgrade' | sudo EDITOR='tee -a' visudo\"")))
{
    TaskService Service = new();
    var Task = Service.NewTask();
    Task.Triggers.Add(new WeeklyTrigger());
    Task.Actions.Add("wsl", "-d Ubuntu sudo apt update && sudo apt dist-upgrade -y && sudo apt autoremove -y && sudo do-release-upgrade");
    Task.Settings.StartWhenAvailable = true;
    Task.Settings.Hidden = true;
    Service.RootFolder.RegisterTaskDefinition(Assembly.GetEntryAssembly()!.GetName().Name!, Task);
}
else
    throw new UnauthorizedAccessException();

string? Run(string Arguments)
{
    StringBuilder Result = new();
    Process Runner = new()
    {
        StartInfo = new ProcessStartInfo("wsl", "-d Ubuntu " + Arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true
        }!
    };
    Runner.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
    {
        if (e.Data is not null)
            Result.Append(e.Data);
    };
    Runner.Start();
    Runner.BeginOutputReadLine();
    Runner.WaitForExit();
    return Result.ToString();
}