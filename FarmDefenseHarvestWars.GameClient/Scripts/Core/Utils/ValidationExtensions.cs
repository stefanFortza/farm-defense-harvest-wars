using Godot;
using System;

namespace FarmDefenseHarvestWars.GameClient.Core.Utils;

public static class ValidationExtensions
{
    public static void EnsureNotNull(this Node context, object? dependency, string propertyName)
    {
        if (dependency == null)
        {
            string errorMsg = $"[FATAL][{context.GetType().Name}] Pe nodul '{context.Name}': Referința '{propertyName}' este NULL. Ai uitat să o legi în Inspector?";

            GD.PrintErr(errorMsg);
            throw new ArgumentNullException(propertyName, errorMsg);
        }
    }
}
