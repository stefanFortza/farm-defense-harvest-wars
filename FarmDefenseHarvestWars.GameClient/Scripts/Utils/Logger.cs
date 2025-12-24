using Godot;
using System;

public static class Logger
{
    public static void Info(string msg) => GD.Print(msg);
    public static void Error(string msg) => GD.PrintErr(msg);
}
