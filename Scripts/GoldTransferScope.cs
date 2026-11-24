using System;
using System.Threading;
using MegaCrit.Sts2.Core.Entities.Players;

namespace GoldLend.Scripts;

/// <summary>
/// Identifies the receiving gold calculation of one GoldLend transfer. Other
/// gains triggered by a relic, even inside the same async flow, stay normal.
/// </summary>
internal sealed class GoldTransferScope : IDisposable
{
    private static readonly AsyncLocal<GoldTransferScope?> Current = new();
    private static readonly AsyncLocal<GoldCalculation?> Calculation = new();

    private readonly Player _recipient;
    private readonly GoldTransferScope? _previous;
    private bool _calculationClaimed;
    private bool _disposed;

    private GoldTransferScope(Player recipient)
    {
        _recipient = recipient;
        _previous = Current.Value;
        Current.Value = this;
    }

    internal static IDisposable Enter(Player recipient) => new GoldTransferScope(recipient);

    internal static IDisposable? BeginCalculation(Player player)
    {
        GoldTransferScope? transfer = Current.Value;
        if (transfer == null)
        {
            return null;
        }

        bool isTransfer = !transfer._calculationClaimed && ReferenceEquals(transfer._recipient, player);
        if (isTransfer)
        {
            // Claim before invoking any modifiers: a reentrant ordinary gain
            // must not steal or inherit this transfer's exemption.
            transfer._calculationClaimed = true;
        }

        var calculation = new GoldCalculation(player, isTransfer, Calculation.Value);
        Calculation.Value = calculation;
        return calculation;
    }

    internal static bool IgnoresBowlerHat(Player player) =>
        Calculation.Value is { IsTransfer: true } calculation
        && ReferenceEquals(calculation.Player, player);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Current.Value = _previous;
        _disposed = true;
    }

    private sealed class GoldCalculation(Player player, bool isTransfer, GoldCalculation? previous) : IDisposable
    {
        private bool _disposed;
        internal Player Player { get; } = player;
        internal bool IsTransfer { get; } = isTransfer;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Calculation.Value = previous;
            _disposed = true;
        }
    }
}
