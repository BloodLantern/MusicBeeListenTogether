using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MusicBeePlugin;

public static class GitCommands
{
    public static string RepositoryPath { get; internal set; }

    public static void UpdateRepository() => RunGitCommand("pull", false, false)?.Start();

    public static string GetLocalConfigUsername()
    {
        Process process = RunGitCommand("config user.name", true, true);
        if (process == null)
            return null;

        string dataReceived = null;
        process.OutputDataReceived += (_, args) => dataReceived = args.Data;

        process.Start();

        process.WaitForExit();

        if (process.ExitCode != 0 || dataReceived == null)
            return null;

        return dataReceived;
    }
    
    private static Process RunGitCommand(string command, bool autoTerminate, bool hidden)
    {
        Process process = new();
        
        // FIXME
        process.StartInfo = new(hidden ? "git.exe" : "cmd.exe", $"{(autoTerminate ? "/C" : "/K")} git {command}")
        {
            WindowStyle = hidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
            WorkingDirectory = RepositoryPath
        };
        
        process.EnableRaisingEvents = true;
        
        return process;
    }
}
