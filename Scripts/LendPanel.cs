using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace GoldLend.Scripts;

internal sealed partial class LendPanel : PanelContainer
{
	private sealed class Selection
	{
		public ulong? NetId { get; set; }
	}

	private sealed record TeammateCard(Player Player, string Name, Button Button, Label Selected)
	{
		public Color? Accent { get; set; }
	}

	private static readonly ConditionalWeakTable<IRunState, Selection> RunSelections = new();
	private static LendPanel? _open;

	private Player _local = null!;
	private NTopBarGold _anchor = null!;
	private Selection _selection = null!;
	private TeammateCard? _target;
	private Label _wallet = null!;
	private Label _preview = null!;
	private LineEdit _editor = null!;
	private Button _lend = null!;
	private Button _request = null!;
	private int _amount = 10;
	private string _lastAmountText = string.Empty;

	public static void Toggle(NTopBarGold gold)
	{
		if (!GodotObject.IsInstanceValid(gold)) return;
		if (_open is { } open && GodotObject.IsInstanceValid(open))
		{
			open.Close();
			if (open._anchor == gold) return;
		}
		if (!GoldUiAnchor.TryGetShopPlayer(out Player local)) return;
		Control? host = GoldUiAnchor.ResolveHost(gold);
		if (host == null) return;

		var teammates = local.RunState.Players.Where(player => player.NetId != local.NetId).ToList();
		var panel = new LendPanel { Name = "GoldLendPanel", ZIndex = 100,
			_local = local, _anchor = gold, _selection = RunSelections.GetValue(local.RunState, static _ => new Selection()) };
		panel.Build(teammates);
		_open = panel;
		host.AddChild(panel);
		panel.Open();
	}

	public static void CloseFor(NTopBarGold gold)
	{
		if (_open is { } open && GodotObject.IsInstanceValid(open) && open._anchor == gold) open.Close();
	}

	public override void _Input(InputEvent input)
	{
		if (!GodotObject.IsInstanceValid(this) || _open != this
			|| input is not InputEventKey { Keycode: Key.Escape, Pressed: true, Echo: false }) return;
		GetViewport().SetInputAsHandled();
		Close();
	}

	private void Build(List<Player> teammates)
	{
		VBoxContainer column = GoldLendUi.Frame(this);
		BuildHeader(column);
		TeammateCard defaultTarget = BuildTeammates(column, teammates);
		BuildAmountControls(column);
		BuildActions(column);
		column.AddChild(GoldLendUi.Text(Loc.T("panel.markHint"), 14, GoldLendUi.Muted));

		SetAmount(_amount);
		Select(defaultTarget);
	}

	private void BuildHeader(VBoxContainer column)
	{
		HBoxContainer header = GoldLendUi.Row(8);
		Label title = GoldLendUi.Text(Loc.T("panel.title"), 26, StsColors.gold);
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		header.AddChild(title);

		var wallet = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter };
		wallet.AddThemeStyleboxOverride("panel", GoldLendUi.WalletBox());
		HBoxContainer walletRow = GoldLendUi.Row(5);
		walletRow.MouseFilter = MouseFilterEnum.Ignore;
		walletRow.AddChild(GoldLendUi.Coin(19));
		_wallet = GoldLendUi.Text(string.Empty, 18, StsColors.gold);
		walletRow.AddChild(_wallet);
		wallet.AddChild(walletRow);
		header.AddChild(wallet);

		Button close = GoldLendUi.ActionButton("button.closeSymbol");
		close.TooltipText = Loc.T("button.close");
		close.CustomMinimumSize = new Vector2(30, 30);
		close.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		close.AddThemeFontSizeOverride("font_size", 18);
		GoldLendUi.BindPress(close, Close);
		header.AddChild(close);
		column.AddChild(header);
	}

	private TeammateCard BuildTeammates(VBoxContainer column, List<Player> teammates)
	{
		var grid = new GridContainer
		{
			Columns = teammates.Count > 3 ? 2 : teammates.Count,
			SizeFlagsHorizontal = teammates.Count == 1 ? SizeFlags.ShrinkCenter : SizeFlags.ExpandFill
		};
		grid.AddThemeConstantOverride("h_separation", 8);
		grid.AddThemeConstantOverride("v_separation", 8);
		column.AddChild(grid);

		var group = new ButtonGroup { AllowUnpress = false };
		TeammateCard? first = null;
		TeammateCard? remembered = null;
		foreach (Player teammate in teammates)
		{
			string name = GoldUiAnchor.DisplayName(teammate);
			var button = new Button
			{
				ToggleMode = true,
				ButtonGroup = group,
				AccessibilityName = name,
				TooltipText = name,
				MouseDefaultCursorShape = CursorShape.PointingHand,
				CustomMinimumSize = new Vector2(teammates.Count == 1 ? 230 : 144, 96),
				SizeFlagsHorizontal = SizeFlags.ExpandFill
			};
			GoldLendUi.StyleTeammate(button);
			grid.AddChild(button);

			var content = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
			content.AddThemeConstantOverride("separation", 4);
			button.AddChild(content);
			content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
			content.OffsetLeft = 10;
			content.OffsetRight = -10;
			content.OffsetTop = 8;
			content.OffsetBottom = -8;

			HBoxContainer identity = GoldLendUi.Row(5);
			identity.MouseFilter = MouseFilterEnum.Ignore;
			identity.CustomMinimumSize = new Vector2(0, 24);
			Label selected = GoldLendUi.Text(Loc.T("panel.selected"), 13, StsColors.gold);
			selected.Visible = false;
			selected.HorizontalAlignment = HorizontalAlignment.Right;
			selected.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			selected.VerticalAlignment = VerticalAlignment.Center;
			Label playerName = GoldLendUi.Text(name, 18, StsColors.cream);
			playerName.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			var mark = new Panel
			{
				CustomMinimumSize = new Vector2(7, 7),
				SizeFlagsVertical = SizeFlags.ShrinkCenter,
				MouseFilter = MouseFilterEnum.Ignore
			};
			HBoxContainer balance = GoldLendUi.Amount(teammate.Gold, out Label number, 17, coinFirst: true);
			balance.MouseFilter = MouseFilterEnum.Ignore;
			var card = new TeammateCard(teammate, name, button, selected);
			PlayerGoldBadge.AddIdentity(identity, _local.RunState, teammate.NetId, teammate, playerName, number, button,
				accent =>
				{
					card.Accent = accent;
					if (GodotObject.IsInstanceValid(mark))
					{
						mark.Visible = accent.HasValue;
						if (accent is Color color) mark.AddThemeStyleboxOverride("panel", GoldLendUi.Solid(color));
					}
					if (ReferenceEquals(_target, card)) RefreshActionColors();
				});
			identity.AddChild(mark);
			identity.AddChild(selected);
			content.AddChild(identity);
			content.AddChild(playerName);
			content.AddChild(balance);
			PlayerGoldBadge.BindBalance(teammate, number);
			GoldLendUi.BindPress(button, () => Select(card));

			first ??= card;
			if (teammate.NetId == _selection.NetId) remembered = card;
		}
		return remembered ?? first!;
	}

	private void BuildAmountControls(VBoxContainer column)
	{
		HBoxContainer amountRow = GoldLendUi.Row(10);
		HBoxContainer stepper = GoldLendUi.Row(4);
		stepper.MouseFilter = MouseFilterEnum.Stop;
		Button minus = SmallButton("button.minus");
		Button plus = SmallButton("button.plus");
		string stepHint = Loc.T("panel.stepHint");
		minus.TooltipText = plus.TooltipText = stepHint;
		GoldLendUi.BindPress(minus, () => Step(-1, Input.IsKeyPressed(Key.Shift), Input.IsKeyPressed(Key.Ctrl)));
		GoldLendUi.BindPress(plus, () => Step(1, Input.IsKeyPressed(Key.Shift), Input.IsKeyPressed(Key.Ctrl)));

		var field = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
		field.AddThemeStyleboxOverride("panel", GoldLendUi.AmountFieldBox());
		HBoxContainer fieldRow = GoldLendUi.Row(2);
		fieldRow.MouseFilter = MouseFilterEnum.Ignore;
		fieldRow.AddChild(GoldLendUi.Coin(18));
		_editor = new LineEdit
		{
			Alignment = HorizontalAlignment.Center,
			CustomMinimumSize = new Vector2(96, 28),
			TooltipText = Loc.T("panel.amount") + "\n" + stepHint
		};
		_editor.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
		_editor.AddThemeColorOverride("font_color", StsColors.gold);
		_editor.AddThemeFontSizeOverride("font_size", 20);
		_editor.TextChanged += EditAmount;
		_editor.FocusExited += CommitAmount;
		_editor.TreeExiting += () =>
		{
			_editor.TextChanged -= EditAmount;
			_editor.FocusExited -= CommitAmount;
		};
		fieldRow.AddChild(_editor);
		field.AddChild(fieldRow);
		stepper.AddChild(minus);
		stepper.AddChild(field);
		stepper.AddChild(plus);

		void Wheel(InputEvent input)
		{
			if (!GodotObject.IsInstanceValid(stepper)
				|| input is not InputEventMouseButton { Pressed: true } wheel
				|| wheel.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown)) return;
			stepper.AcceptEvent();
			Step(wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1, wheel.ShiftPressed, wheel.CtrlPressed);
		}
		stepper.GuiInput += Wheel;
		stepper.TreeExiting += () => stepper.GuiInput -= Wheel;

		amountRow.AddChild(stepper);
		_preview = GoldLendUi.Text(string.Empty, 17, StsColors.cream);
		_preview.HorizontalAlignment = HorizontalAlignment.Right;
		_preview.VerticalAlignment = VerticalAlignment.Center;
		_preview.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		amountRow.AddChild(_preview);
		column.AddChild(amountRow);

		HBoxContainer chips = GoldLendUi.Row(5);
		foreach (int delta in new[] { 10, 25, 50, 100 })
		{
			Button chip = AddChip("button.chip", delta);
			GoldLendUi.BindPress(chip, () => SetAmount(GoldAmountMath.Add(_amount, delta)));
		}
		GoldLendUi.BindPress(AddChip("button.all"), () => SetAmount(GoldAmountMath.All(_local.Gold)));
		GoldLendUi.BindPress(AddChip("button.clear"), () => SetAmount(GoldAmountMath.Clear()));
		column.AddChild(chips);

		Button AddChip(string key, params object[] args)
		{
			Button chip = SmallButton(key);
			chip.Text = Loc.T(key, args);
			chip.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			chips.AddChild(chip);
			return chip;
		}
	}

	private static Button SmallButton(string key)
	{
		Button button = GoldLendUi.ActionButton(key);
		button.CustomMinimumSize = new Vector2(32, 32);
		button.AddThemeFontSizeOverride("font_size", 16);
		return button;
	}

	private void BuildActions(VBoxContainer column)
	{
		_lend = GoldLendUi.ActionButton("button.lend", primary: true);
		_request = GoldLendUi.ActionButton("button.request");
		HBoxContainer row = GoldLendUi.Row(8);
		foreach (Button button in new[] { _lend, _request })
		{
			button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			button.CustomMinimumSize = new Vector2(216, 46);
			button.ClipText = true;
			button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			button.AddThemeFontSizeOverride("font_size", 17);
			row.AddChild(button);
		}
		GoldLendUi.BindPress(_lend, () => Send(lend: true));
		GoldLendUi.BindPress(_request, () => Send(lend: false));
		column.AddChild(row);
	}

	private void Open()
	{
		Viewport viewport = GetViewport();
		_local.GoldChanged += RefreshWallet;
		_anchor.ItemRectChanged += Place;
		viewport.SizeChanged += Place;
		TreeExiting += () =>
		{
			_local.GoldChanged -= RefreshWallet;
			if (GodotObject.IsInstanceValid(_anchor)) _anchor.ItemRectChanged -= Place;
			if (GodotObject.IsInstanceValid(viewport)) viewport.SizeChanged -= Place;
			if (_open == this) _open = null;
		};
		RefreshWallet();
		Place();
		Callable.From(Place).CallDeferred();

		Modulate = new Color(1f, 1f, 1f, 0f);
		Scale = new Vector2(0.98f, 0.98f);
		Tween entry = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		entry.TweenProperty(this, "modulate:a", 1f, 0.12);
		entry.TweenProperty(this, "scale", Vector2.One, 0.12);
	}

	private void Place()
	{
		if (!GodotObject.IsInstanceValid(this) || _open != this || !IsInsideTree()
			|| !GodotObject.IsInstanceValid(_anchor)) return;
		GoldLendUi.KeepInView(this, _anchor.GlobalPosition + new Vector2(0, _anchor.Size.Y + 8));
	}

	private void Select(TeammateCard card)
	{
		if (!GodotObject.IsInstanceValid(card.Button) || !GodotObject.IsInstanceValid(card.Selected)) return;
		if (_target is { } previous && GodotObject.IsInstanceValid(previous.Selected)) previous.Selected.Hide();
		_target = card;
		_selection.NetId = card.Player.NetId;
		card.Button.SetPressedNoSignal(true);
		card.Selected.Show();
		RefreshActionColors();
		RefreshActions();
	}

	private void RefreshActionColors()
	{
		if (!GodotObject.IsInstanceValid(_lend) || !GodotObject.IsInstanceValid(_request)) return;
		Color text = _target?.Accent ?? StsColors.cream;
		GoldLendUi.StyleButton(_lend, text, primary: true);
		GoldLendUi.StyleButton(_request, text);
	}

	private void EditAmount(string text)
	{
		if (!GodotObject.IsInstanceValid(_editor)) return;
		int caret = _editor.CaretColumn;
		if (!GoldAmountMath.TryParse(text, out int amount))
		{
			_editor.Text = _lastAmountText;
			_editor.CaretColumn = Math.Min(caret, _lastAmountText.Length);
			return;
		}
		if (amount == GoldAmountMath.Maximum)
		{
			SetAmount(amount);
			_editor.CaretColumn = Math.Min(caret, _editor.Text.Length);
			return;
		}
		_lastAmountText = text;
		_amount = amount;
		RefreshActions();
	}

	private void CommitAmount()
	{
		if (_open == this) SetAmount(_amount);
	}

	private void Step(int direction, bool shift, bool control) =>
		SetAmount(GoldAmountMath.Add(_amount, direction * GoldAmountMath.StepSize(shift, control)));

	private void SetAmount(int amount)
	{
		if (!GodotObject.IsInstanceValid(_editor)) return;
		_amount = amount;
		_lastAmountText = amount.ToString();
		if (_editor.Text != _lastAmountText)
		{
			_editor.Text = _lastAmountText;
			_editor.CaretColumn = _lastAmountText.Length;
		}
		RefreshActions();
	}

	private void RefreshWallet()
	{
		if (!GodotObject.IsInstanceValid(_wallet)) return;
		_wallet.Text = Loc.T("panel.wallet", _local.Gold);
		RefreshActions();
	}

	private void RefreshActions()
	{
		if (_target is not { } target || !GodotObject.IsInstanceValid(_preview)
			|| !GodotObject.IsInstanceValid(_lend) || !GodotObject.IsInstanceValid(_request)) return;

		int? remaining = GoldAmountMath.Remaining(_amount, _local.Gold);
		_preview.Text = remaining is int balance ? Loc.T("panel.remaining", balance) : Loc.T("panel.insufficient", _local.Gold);
		_preview.AddThemeColorOverride("font_color", remaining.HasValue ? StsColors.cream : GoldLendUi.Warning);
		_preview.TooltipText = _preview.Text;
		_lend.Text = Loc.T("button.lendTo", _amount, target.Name);
		_request.Text = Loc.T("button.requestFrom", target.Name, _amount);
		_lend.TooltipText = _lend.Text;
		_request.TooltipText = _request.Text;
		_lend.Disabled = !GoldAmountMath.CanLend(_amount, _local.Gold);
		_request.Disabled = !GoldAmountMath.CanRequest(_amount);
	}

	private void Send(bool lend)
	{
		if (!GodotObject.IsInstanceValid(this) || _open != this || _target is not { } target
			|| !GoldUiAnchor.TryGetShopPlayer(out Player local) || local != _local
			|| !local.RunState.Players.Contains(target.Player)) return;
		INetGameService? netService = RunManager.Instance?.NetService;
		if (netService == null) return;

		bool allowed = lend ? GoldAmountMath.CanLend(_amount, local.Gold) : GoldAmountMath.CanRequest(_amount);
		if (!allowed) return;
		ulong targetNetId = target.Player.NetId;
		bool sent = lend ? LendNetAction.Request(netService.NetId, targetNetId, _amount)
			: GoldRequestMessages.SendRequest(netService.NetId, targetNetId, _amount);
		if (!sent) Entry.Logger.Warn(lend ? "借出请求入队失败。" : "索要请求发送失败。");
	}

	private void Close()
	{
		if (!GodotObject.IsInstanceValid(this) || _open != this) return;
		_open = null;
		SetProcessInput(false);
		Hide();
		QueueFree();
	}
}
