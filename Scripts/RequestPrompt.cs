using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace GoldLend.Scripts;

internal static class RequestPrompt
{
	private const string PanelName = "GoldLendRequestPrompt";

	private static readonly Queue<GoldRequestMessage> Pending = new Queue<GoldRequestMessage>();

	private static PanelContainer? _panel;
	private static Label? _title;
	private static int _position;

	private static GoldRequestMessage? _current;

	public static void Enqueue(GoldRequestMessage request)
	{
		Pending.Enqueue(request);
		if (_current.HasValue) RefreshTitle();
		else TryShowNext();
	}

	private static void TryShowNext()
	{
		if (_current.HasValue)
		{
			return;
		}
		while (Pending.Count > 0)
		{
			GoldRequestMessage next = Pending.Dequeue();
			Player? me = GoldUiAnchor.LocalPlayer();
			if (me == null || me.NetId != next.TargetNetId || next.Amount <= 0)
			{
				Entry.Logger.Info("丢弃一条无效或过期的索要请求。");
				continue;
			}
			_current = next;
			_position++;
			Show(next, me);
			return;
		}
		_position = 0;
	}

	private static void RefreshTitle()
	{
		if (_title is not { } title || !GodotObject.IsInstanceValid(title)) return;
		int total = _position + Pending.Count;
		title.Text = total > 1 ? Loc.T("request.progress", _position, total) : Loc.T("request.title");
	}

	private static void Show(GoldRequestMessage request, Player me)
	{
		NTopBarGold? gold = GoldUiAnchor.Gold;
		Control? host = (gold == null) ? null : GoldUiAnchor.ResolveHost(gold);
		if (host == null)
		{
			Entry.Logger.Warn("未找到可挂载的顶栏节点，索要弹窗无法显示。");
			Dismiss();
			return;
		}
		IRunState state = me.RunState;
		Player? requester = state.Players.FirstOrDefault(p => p.NetId == request.RequesterNetId);
		var panel = new PanelContainer
		{
			Name = PanelName,
			CustomMinimumSize = new Vector2(390, 0),
			ZIndex = 200
		};
		VBoxContainer column = GoldLendUi.Frame(panel, 12);
		_title = GoldLendUi.AddDragTitle(column, panel, "request.title");
		RefreshTitle();
		HBoxContainer head = GoldLendUi.Card(column, highlighted: true);
		HBoxContainer requested = GoldLendUi.Amount(request.Amount, out Label requestedGold, 24);
		string name = requester != null ? GoldUiAnchor.DisplayName(requester) : Loc.T("player.unknown");
		Label requestText = GoldLendUi.PlayerName(name);
		PlayerGoldBadge.AddIdentity(head, state, request.RequesterNetId, requester, requestText, requestedGold);
		head.AddChild(requestText);
		head.AddChild(requested);
		column.AddChild(GoldLendUi.Text(Loc.T("request.confirm"), 18, StsColors.cream));

		HBoxContainer funds = GoldLendUi.Row(6);
		funds.AddChild(GoldLendUi.Text(Loc.T("request.insufficient"), 16, GoldLendUi.Muted));
		funds.AddChild(GoldLendUi.Amount(me.Gold, out Label available, 18));
		column.AddChild(funds);

		Button give = GoldLendUi.ActionButton("button.give", primary: true);
		GoldLendUi.BindPress(give, () => OnAnswer(panel, request, me, accepted: true));
		Button deny = GoldLendUi.ActionButton("button.deny");
		GoldLendUi.BindPress(deny, () => OnAnswer(panel, request, me, accepted: false));
		give.SizeFlagsHorizontal = deny.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		HBoxContainer buttons = GoldLendUi.Row(10);
		buttons.AddChild(give);
		buttons.AddChild(deny);
		column.AddChild(buttons);

		void RefreshFunds()
		{
			if (!GodotObject.IsInstanceValid(give) || !GodotObject.IsInstanceValid(funds)
				|| !GodotObject.IsInstanceValid(available)) return;
			int payable = GoldAmountMath.RequestPayment(request.Amount, me.Gold);
			give.Disabled = payable == 0;
			give.Text = payable > 0 && payable < request.Amount ? Loc.T("request.giveAll", payable) : Loc.T("button.give");
			funds.Visible = payable < request.Amount;
			available.Text = me.Gold.ToString();
		}
		me.GoldChanged += RefreshFunds;
		panel.TreeExiting += () =>
		{
			me.GoldChanged -= RefreshFunds;
			if (_panel != panel) return;
			_panel = null;
			_title = null;
			_position = 0;
			_current = null;
			Pending.Clear();
		};
		RefreshFunds();
		host.AddChild(panel);
		_panel = panel;

		void PlacePrompt()
		{
			if (!GodotObject.IsInstanceValid(panel) || !panel.IsInsideTree() || _panel != panel) return;
			Rect2 visible = panel.GetCanvasTransform().AffineInverse() * panel.GetViewportRect();
			float width = Mathf.Max(panel.Size.X, panel.GetCombinedMinimumSize().X);
			GoldLendUi.KeepInView(panel, new Vector2(visible.GetCenter().X - width / 2, visible.Position.Y + 142));
		}
		PlacePrompt();
		Callable.From(PlacePrompt).CallDeferred();
		Entry.Logger.Info("索要弹窗已显示。");
	}

	private static void OnAnswer(PanelContainer panel, GoldRequestMessage request, Player me, bool accepted)
	{
		if (_panel != panel || !GodotObject.IsInstanceValid(panel) || GoldUiAnchor.LocalPlayer() != me) return;
		int amount = request.Amount;
		if (accepted)
		{
			amount = GoldAmountMath.RequestPayment(request.Amount, me.Gold);
			if (amount == 0) return;
			if (!LendNetAction.Request(me.NetId, request.RequesterNetId, amount))
			{
				Entry.Logger.Warn("给予请求入队失败。");
				return;
			}
		}
		GoldRequestMessages.SendReply(request.RequesterNetId, request.TargetNetId, amount, accepted);
		Dismiss();
	}

	private static void Dismiss()
	{
		_current = null;
		PanelContainer? dismissed = _panel;
		_panel = null;
		_title = null;
		if (dismissed != null && GodotObject.IsInstanceValid(dismissed))
		{
			dismissed.Hide();
			dismissed.QueueFree();
		}
		TryShowNext();
	}
}
