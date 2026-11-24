using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;

namespace GoldLend.Scripts;

internal static class RequestReplyNotice
{
	private const double Duration = 3.5;
	private const int MaxToasts = 3;
	private const int FontSize = 17;
	private const int MaxTextLines = 2;
	private static VBoxContainer? _stack;

	public static void Show(GoldRequestReplyMessage reply, Player local)
	{
		VBoxContainer? stack = ResolveStack();
		if (stack == null) return;

		Player? teammate = local.RunState.Players.FirstOrDefault(player => player.NetId == reply.TargetNetId);
		string name = teammate != null ? GoldUiAnchor.DisplayName(teammate) : Loc.T("player.unknown");
		string text = Loc.T(reply.Accepted ? "toast.accepted" : "toast.declined", name, reply.Amount);
		Add(stack, text, reply.Accepted ? GoldLendUi.Positive : GoldLendUi.Negative);
	}

	public static void ShowArrival(Player from, int amount)
	{
		VBoxContainer? stack = ResolveStack();
		if (stack != null) Add(stack, Loc.T("toast.arrived", GoldUiAnchor.DisplayName(from), amount), GoldLendUi.Positive);
	}

	private static VBoxContainer? ResolveStack()
	{
		Control? host = GoldUiAnchor.Gold is { } gold ? GoldUiAnchor.ResolveHost(gold) : null;
		if (host == null) return null;
		if (_stack is { } existing && GodotObject.IsInstanceValid(existing) && existing.IsInsideTree())
		{
			if (existing.GetParent() == host) return existing;
			existing.Hide();
			existing.QueueFree();
		}

		var stack = new VBoxContainer
		{
			Name = "GoldLendToasts",
			ZIndex = 190,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ProcessMode = Node.ProcessModeEnum.Always
		};
		stack.AddThemeConstantOverride("separation", 8);
		host.AddChild(stack);
		_stack = stack;
		Viewport viewport = stack.GetViewport();
		void Fit() => FitStack(stack);
		stack.MinimumSizeChanged += Fit;
		viewport.SizeChanged += Fit;
		stack.TreeExiting += () =>
		{
			stack.MinimumSizeChanged -= Fit;
			if (GodotObject.IsInstanceValid(viewport)) viewport.SizeChanged -= Fit;
			if (_stack == stack) _stack = null;
		};
		return stack;
	}

	private static void Add(VBoxContainer stack, string text, Color status)
	{
		if (!GodotObject.IsInstanceValid(stack) || !stack.IsInsideTree()) return;
		if (stack.GetChildCount() >= MaxToasts && stack.GetChild(0) is Control oldest && GodotObject.IsInstanceValid(oldest))
		{
			stack.RemoveChild(oldest);
			oldest.QueueFree();
		}

		var notice = new PanelContainer
		{
			CustomMinimumSize = new Vector2(330, 46),
			MouseFilter = Control.MouseFilterEnum.Stop,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand
		};
		notice.AddThemeStyleboxOverride("panel", GoldLendUi.ToastBox());
		var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
		margin.AddThemeConstantOverride("margin_left", 17);
		margin.AddThemeConstantOverride("margin_right", 12);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_bottom", 12);
		Label label = GoldLendUi.Text(text, FontSize, StsColors.cream);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		label.MaxLinesVisible = MaxTextLines;
		label.SizeFlagsVertical = Control.SizeFlags.Fill;
		label.VerticalAlignment = VerticalAlignment.Center;
		margin.AddChild(label);
		notice.AddChild(margin);

		var overlay = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
		notice.AddChild(overlay);
		var stripe = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
		stripe.AddThemeStyleboxOverride("panel", GoldLendUi.Solid(status));
		overlay.AddChild(stripe);
		stripe.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.LeftWide);
		stripe.OffsetLeft = 5;
		stripe.OffsetRight = 9;
		stripe.OffsetTop = 6;
		stripe.OffsetBottom = -6;

		var progress = new ProgressBar
		{
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ShowPercentage = false, MaxValue = 1, Step = 0, Value = 1
		};
		progress.AddThemeStyleboxOverride("background", new StyleBoxEmpty());
		progress.AddThemeStyleboxOverride("fill", GoldLendUi.Solid(StsColors.gold));
		overlay.AddChild(progress);
		progress.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
		progress.OffsetLeft = 10;
		progress.OffsetRight = -5;
		progress.OffsetTop = -3;
		progress.OffsetBottom = -1;

		stack.AddChild(notice);
		// A wrapped Label can report a 1px minimum before layout gives it a width.
		float lineHeight = label.GetThemeFont("font").GetHeight(FontSize);
		int lineSpacing = label.GetThemeConstant("line_spacing");
		label.CustomMinimumSize = new Vector2(0, Mathf.Ceil(lineHeight * MaxTextLines + lineSpacing));
		FitStack(stack);
		Tween lifetime = notice.CreateTween().SetIgnoreTimeScale();
		lifetime.TweenProperty(progress, "value", 0.0, Duration);
		lifetime.TweenProperty(notice, "modulate:a", 0f, 0.18);
		lifetime.TweenCallback(Callable.From(Dismiss));

		void Dismiss()
		{
			if (!GodotObject.IsInstanceValid(notice) || notice.IsQueuedForDeletion()) return;
			notice.Hide();
			notice.QueueFree();
		}
		void Pause()
		{
			if (GodotObject.IsInstanceValid(lifetime) && lifetime.IsValid()) lifetime.Pause();
		}
		void Resume()
		{
			if (GodotObject.IsInstanceValid(lifetime) && lifetime.IsValid()) lifetime.Play();
		}
		void OnClick(InputEvent input)
		{
			if (!GodotObject.IsInstanceValid(notice)
				|| input is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
			notice.AcceptEvent();
			Dismiss();
		}
		notice.MouseEntered += Pause;
		notice.MouseExited += Resume;
		notice.GuiInput += OnClick;
		notice.TreeExiting += () =>
		{
			notice.MouseEntered -= Pause;
			notice.MouseExited -= Resume;
			notice.GuiInput -= OnClick;
			if (GodotObject.IsInstanceValid(lifetime) && lifetime.IsValid()) lifetime.Kill();
		};
	}

	private static void FitStack(VBoxContainer stack)
	{
		if (!GodotObject.IsInstanceValid(stack) || !stack.IsInsideTree()) return;
		stack.ResetSize();
		Rect2 visible = stack.GetCanvasTransform().AffineInverse() * stack.GetViewportRect();
		Vector2 size = stack.Size.Max(stack.GetCombinedMinimumSize());
		GoldLendUi.KeepInView(stack, visible.End - size - new Vector2(24, 72));
	}
}
