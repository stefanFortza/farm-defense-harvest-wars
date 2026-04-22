using System;
using Godot;

namespace FarmDefenseHarvestWars.GameClient.Core.Utils;

public static class ToastNotifications
{
    public static bool TryShowLoading(string message, out string toastId)
    {
        toastId = string.Empty;

        if (!TryGetToastX(out var toastX))
        {
            return false;
        }

        try
        {
            Variant result = toastX.Call("loading", message);
            toastId = result.VariantType == Variant.Type.String ? result.AsString() : string.Empty;
            return !string.IsNullOrWhiteSpace(toastId);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"ToastX loading toast failed: {ex.Message}");
            return false;
        }
    }

    public static bool TryDismiss(string toastId)
    {
        if (string.IsNullOrWhiteSpace(toastId))
        {
            return false;
        }

        if (!TryGetToastX(out var toastX))
        {
            return false;
        }

        try
        {
            Variant result = toastX.Call("dismiss", toastId);
            return result.VariantType == Variant.Type.Bool && result.AsBool();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"ToastX dismiss failed: {ex.Message}");
            return false;
        }
    }

    public static bool TrySuccess(string message, double seconds)
    {
        return TryQuickToast("success", message, seconds);
    }

    public static bool TryError(string message, double seconds)
    {
        return TryQuickToast("error", message, seconds);
    }

    public static bool TryInfo(string message, double seconds)
    {
        return TryQuickToast("info", message, seconds);
    }

    private static bool TryQuickToast(string method, string message, double seconds)
    {
        if (!TryGetToastX(out var toastX))
        {
            return false;
        }

        try
        {
            toastX.Call(method, message, seconds);
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"ToastX '{method}' call failed: {ex.Message}");
            return false;
        }
    }

    private static bool TryGetToastX(out Node toastX)
    {
        toastX = null!;

        var tree = Engine.GetMainLoop() as SceneTree;
        var root = tree?.Root;
        if (root == null)
        {
            return false;
        }

        var singleton = root.GetNodeOrNull<Node>("/root/ToastX");
        if (singleton == null)
        {
            return false;
        }

        toastX = singleton;
        return true;
    }
}