using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Relics;

namespace GoldLend.Scripts;

// Hook.ModifyGoldGained is synchronous. Its invocation frame keeps nested
// normal rewards separate without replacing the game's modifier pipeline.
[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyGoldGained))]
internal static class GoldTransferCalculationPatch
{
    [HarmonyPrefix]
    private static void Prefix(Player player, out IDisposable? __state)
    {
        __state = GoldTransferScope.BeginCalculation(player);
    }

    [HarmonyFinalizer]
    private static void Finalizer(IDisposable? __state)
    {
        // Finalizers also run when a third-party modifier throws. Do not
        // swallow the exception or let the exemption affect later rewards.
        __state?.Dispose();
    }
}

[HarmonyPatch(typeof(BowlerHat), nameof(BowlerHat.ModifyGoldGained))]
internal static class BowlerHatGoldGainPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Player player, decimal amount, ref decimal __result)
    {
        if (!GoldTransferScope.IgnoresBowlerHat(player))
        {
            return true;
        }

        __result = amount;
        return false;
    }
}
