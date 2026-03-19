using Godot;
using System.Collections.Generic;
using System.Linq;

namespace FarmDefenseHarvestWars.GameClient.Scripts.Utils;

public static class CmdArgs
{
    public static string? Email { get; private set; }
    public static string? Password { get; private set; }
    public static bool IsServer { get; private set; }
    public static int? Port { get; private set; }

    static CmdArgs()
    {
        Parse();
    }

    public static void Parse()
    {
        string[] engineArgs = OS.GetCmdlineArgs();
        string[] userArgs = OS.GetCmdlineUserArgs();


        IsServer = engineArgs.Contains("--server") || userArgs.Contains("--server");

        foreach (string arg in userArgs)
        {
            if (arg.StartsWith("--email="))
            {
                Email = arg.Substring("--email=".Length);
            }
            else if (arg.StartsWith("--password="))
            {
                Password = arg.Substring("--password=".Length);
            }
            else if (arg.StartsWith("--port=") && int.TryParse(arg.Substring("--port=".Length), out int parsedPort))
            {
                Port = parsedPort;
            }
        }
    }
}
