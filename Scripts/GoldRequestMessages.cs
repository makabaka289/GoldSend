using System;
using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Networking.Sidecar;

namespace GoldLend.Scripts;

public readonly record struct GoldRequestMessage(ulong RequesterNetId, ulong TargetNetId, int Amount);

public readonly record struct GoldRequestReplyMessage(ulong RequesterNetId, ulong TargetNetId, int Amount, bool Accepted);

public static class GoldRequestMessages
{
	private static readonly RitsuLibSidecarMessageDescriptor<GoldRequestMessage> RequestDescriptor = new RitsuLibSidecarMessageDescriptor<GoldRequestMessage>("GoldLend", "gold_request_v1", (GoldRequestMessage msg) => JsonSerializer.SerializeToUtf8Bytes(msg), (ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<GoldRequestMessage>(bytes), RitsuLibSidecarDeliverySemantics.StableSync, false);

	private static readonly RitsuLibSidecarMessageDescriptor<GoldRequestReplyMessage> ReplyDescriptor = new RitsuLibSidecarMessageDescriptor<GoldRequestReplyMessage>("GoldLend", "gold_request_reply_v1", (GoldRequestReplyMessage msg) => JsonSerializer.SerializeToUtf8Bytes(msg), (ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<GoldRequestReplyMessage>(bytes), RitsuLibSidecarDeliverySemantics.StableSync, false);

	private static IDisposable? _requestSub;

	private static IDisposable? _replySub;

	public static void Subscribe()
	{
		if (_requestSub == null)
		{
			_requestSub = RitsuLibSidecarTypedMessageRegistry.Subscribe<GoldRequestMessage>(RequestDescriptor, OnRequestReceived);
		}
		if (_replySub == null)
		{
			_replySub = RitsuLibSidecarTypedMessageRegistry.Subscribe<GoldRequestReplyMessage>(ReplyDescriptor, OnReplyReceived);
		}
	}

	public static bool SendRequest(ulong requesterNetId, ulong targetNetId, int amount)
	{
		return Dispatch(RequestDescriptor, new GoldRequestMessage(requesterNetId, targetNetId, amount));
	}

	public static bool SendReply(ulong requesterNetId, ulong targetNetId, int amount, bool accepted)
	{
		return Dispatch(ReplyDescriptor, new GoldRequestReplyMessage(requesterNetId, targetNetId, amount, accepted));
	}

	private static bool Dispatch<T>(RitsuLibSidecarMessageDescriptor<T> descriptor, T message)
	{
		INetGameService? netService = RunManager.Instance?.NetService;
		if (netService == null)
		{
			return false;
		}
		switch (netService.Type)
		{
			case NetGameType.Client:
				RitsuLibSidecarTypedMessageRegistry.SendToHost(netService, descriptor, message);
				break;
			default:
				RitsuLibSidecarTypedMessageRegistry.Broadcast(netService, descriptor, message);
				break;
		}
		return true;
	}

	private static void OnRequestReceived(RitsuLibSidecarTypedDispatchContext<GoldRequestMessage> context)
	{
		RelayIfHost(RequestDescriptor, context.Message, context.IsHostIngest);
		Player? me = GoldUiAnchor.LocalPlayer();
		if (me != null && me.NetId == context.Message.TargetNetId)
		{
			Entry.Logger.Info($"收到索要：{context.Message.RequesterNetId} 想要 {context.Message.Amount} 金币。");
			RequestPrompt.Enqueue(context.Message);
		}
	}

	private static void OnReplyReceived(RitsuLibSidecarTypedDispatchContext<GoldRequestReplyMessage> context)
	{
		RelayIfHost(ReplyDescriptor, context.Message, context.IsHostIngest);
		Player? me = GoldUiAnchor.LocalPlayer();
		if (me != null && me.NetId == context.Message.RequesterNetId)
		{
			RequestReplyNotice.Show(context.Message, me);
			Entry.Logger.Info($"索要应答：{(context.Message.Accepted ? "同意" : "拒绝")} {context.Message.Amount} 金币。");
		}
	}

	private static void RelayIfHost<T>(RitsuLibSidecarMessageDescriptor<T> descriptor, T message, bool isHostIngest)
	{
		if (isHostIngest)
		{
			INetGameService? netService = RunManager.Instance?.NetService;
			if (netService != null)
			{
				RitsuLibSidecarTypedMessageRegistry.Broadcast(netService, descriptor, message);
			}
		}
	}
}
