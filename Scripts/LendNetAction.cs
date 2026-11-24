using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.ManagedActions;

namespace GoldLend.Scripts;

public readonly record struct LendGoldMessage(ulong FromNetId, ulong ToNetId, int Amount);

public static class LendNetAction
{
	private static readonly RitsuLibManagedNetActionDescriptor<LendGoldMessage> Descriptor = new RitsuLibManagedNetActionDescriptor<LendGoldMessage>("GoldLend", "lend_gold", (LendGoldMessage msg) => JsonSerializer.SerializeToUtf8Bytes(msg), (ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<LendGoldMessage>(bytes), ExecuteAsync, GameActionType.NonCombat);

	public static void Register()
	{
		RitsuLibManagedNetActions.Register<LendGoldMessage>(Descriptor);
	}

	public static bool Request(ulong fromNetId, ulong toNetId, int amount)
	{
		RunManager? runManager = RunManager.Instance;
		if (runManager == null)
		{
			return false;
		}
		return RitsuLibManagedNetActions.Request<LendGoldMessage>(runManager, Descriptor, new LendGoldMessage(fromNetId, toNetId, amount), null);
	}

	private static async Task ExecuteAsync(RitsuLibManagedNetActionContext<LendGoldMessage> context)
	{
		IRunState? runState = context.Player?.RunState;
		if (runState == null)
		{
			return;
		}
		Player? from = FindPlayer(runState, context.Message.FromNetId);
		Player? to = FindPlayer(runState, context.Message.ToNetId);
		if (from != null && to != null && from != to)
		{
			int amount = Math.Min(context.Message.Amount, from.Gold);
			if (amount > 0)
			{
				await PlayerCmd.LoseGold(amount, from);
				int previousGold = to.Gold;
				// A gift (including an accepted request) is not newly earned
				// gold for BowlerHat. Keep the normal commands and queue sync.
				using (GoldTransferScope.Enter(to))
				{
					await PlayerCmd.GainGold(amount, to);
				}
				GoldArrivalAlert.Notify(to, previousGold);
				int actualAmount = to.Gold - previousGold;
				if (actualAmount > 0 && LocalContext.IsMe(to)) RequestReplyNotice.ShowArrival(from, actualAmount);
				Entry.Logger.Info($"Lent {amount} gold: {from.NetId} -> {to.NetId}");
			}
		}
	}

	private static Player? FindPlayer(IRunState runState, ulong netId)
	{
		return runState.Players.FirstOrDefault((Player p) => p.NetId == netId);
	}
}
