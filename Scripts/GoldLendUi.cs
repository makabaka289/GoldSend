using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace GoldLend.Scripts;

internal static class GoldLendUi
{
	public static readonly Color Muted = new("C8B9A1");
	public static readonly Color Warning = new("F0A37F");
	public static readonly Color Positive = new("82C99C");
	public static readonly Color Negative = new("A25760");
	private static readonly Color Ink = new("201A20F9");
	private static readonly Color Bronze = new("86673E");
	private static readonly Color Line = new("534033");
	private static readonly Color CardFill = new("2A2228");
	private static readonly Color SelectedFill = new("3A2B18");
	private static readonly Color SelectedGold = new("F3C451");
	private static readonly Color FieldFill = new("1A161B");
	private static readonly Color ChipBorder = new("665039");

	public static StyleBoxFlat Box(Color fill, Color border, int paddingX = 12, int paddingY = 8)
	{
		var style = new StyleBoxFlat
		{
			BgColor = fill, BorderColor = border,
			ContentMarginLeft = paddingX, ContentMarginRight = paddingX,
			ContentMarginTop = paddingY, ContentMarginBottom = paddingY
		};
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(5);
		return style;
	}

	public static VBoxContainer Frame(PanelContainer panel, int spacing = 10)
	{
		StyleBoxFlat frame = Box(Ink, Bronze, 14, 12);
		frame.BorderWidthTop = 2;
		frame.ShadowColor = new Color(0f, 0f, 0f, 0.55f);
		frame.ShadowSize = 8;
		frame.ShadowOffset = new Vector2(0, 4);
		panel.AddThemeStyleboxOverride("panel", frame);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", spacing);
		panel.AddChild(column);
		return column;
	}

	public static HBoxContainer Row(int spacing)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", spacing);
		return row;
	}

	public static HBoxContainer Card(VBoxContainer parent, bool highlighted = false)
	{
		var plate = new PanelContainer();
		plate.AddThemeStyleboxOverride("panel", Box(highlighted ? SelectedFill : CardFill, Line, 10, 6));
		HBoxContainer row = Row(8);
		plate.AddChild(row);
		parent.AddChild(plate);
		return row;
	}

	public static Label Text(string value, int size, Color color)
	{
		var label = new Label { Text = value, MouseFilter = Control.MouseFilterEnum.Ignore };
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeFontSizeOverride("font_size", size);
		return label;
	}

	public static Label PlayerName(string name)
	{
		Label label = Text(name, 20, StsColors.cream);
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		label.CustomMinimumSize = new Vector2(150, 0);
		label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		return label;
	}

	public static Label AddDragTitle(VBoxContainer column, Control panel, string titleKey)
	{
		HBoxContainer handle = Row(10);
		handle.MouseFilter = Control.MouseFilterEnum.Stop;
		handle.MouseDefaultCursorShape = Control.CursorShape.Move;
		handle.TooltipText = Loc.T("popup.dragHint");
		handle.CustomMinimumSize = new Vector2(0, 34);
		Label title = Text(Loc.T(titleKey), 26, StsColors.gold);
		handle.AddChild(title);
		column.AddChild(handle);

		Vector2? grabOffset = null;
		void Drag(InputEvent input)
		{
			if (!GodotObject.IsInstanceValid(handle) || !GodotObject.IsInstanceValid(panel)) return;
			if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
			{
				grabOffset = button.Pressed ? panel.GetGlobalMousePosition() - panel.GlobalPosition : null;
				handle.AcceptEvent();
			}
			else if (input is InputEventMouseMotion motion && grabOffset is Vector2 offset)
			{
				if ((motion.ButtonMask & MouseButtonMask.Left) == 0)
				{
					grabOffset = null;
					return;
				}
				KeepInView(panel, panel.GetGlobalMousePosition() - offset);
				handle.AcceptEvent();
			}
		}
		handle.GuiInput += Drag;
		handle.TreeExiting += () => handle.GuiInput -= Drag;
		return title;
	}

	public static TextureRect Coin(int size = 22)
	{
		Texture2D? texture = GoldUiAnchor.IconTexture;
		return new TextureRect
		{
			Texture = texture,
			Visible = texture != null,
			CustomMinimumSize = new Vector2(size, size),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}

	public static HBoxContainer Amount(int value, out Label number, int size = 22, bool coinFirst = false)
	{
		HBoxContainer row = Row(4);
		number = Text(value.ToString(), size, StsColors.gold);
		number.HorizontalAlignment = HorizontalAlignment.Right;
		TextureRect coin = Coin(size);
		if (coinFirst) row.AddChild(coin);
		row.AddChild(number);
		if (!coinFirst) row.AddChild(coin);
		return row;
	}

	public static Button ActionButton(string key, bool primary = false)
	{
		var button = new Button
		{
			Text = Loc.T(key),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			CustomMinimumSize = new Vector2(62, 38)
		};
		StyleButton(button, StsColors.cream, primary);
		return button;
	}

	public static void StyleButton(Button button, Color text, bool primary = false, Color? accent = null)
	{
		Color normal = new(primary ? "5E3E1E" : "2C2329");
		Color hover = new(primary ? "765028" : "43353A");
		Color border = accent ?? (primary ? new Color("C9A461") : ChipBorder);
		if (accent is Color color)
		{
			normal = normal.Lerp(color, 0.10f);
			hover = hover.Lerp(color, 0.16f);
		}
		button.AddThemeStyleboxOverride("normal", Box(normal, border, 9, 3));
		button.AddThemeStyleboxOverride("hover", Box(hover, border.Lightened(0.16f), 9, 3));
		button.AddThemeStyleboxOverride("pressed", Box(new Color("231A1C"), accent ?? StsColors.gold, 9, 3));
		button.AddThemeStyleboxOverride("disabled", Box(new Color("292329"), Line, 9, 3));
		button.AddThemeStyleboxOverride("focus", FocusBox(2));
		foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" })
		{
			button.AddThemeColorOverride(state, text);
		}
		button.AddThemeColorOverride("font_disabled_color", new Color("91857B"));
	}

	public static void StyleTeammate(Button card)
	{
		StyleBoxFlat selected = Box(SelectedFill, SelectedGold, 0, 0);
		selected.SetBorderWidthAll(2);
		card.AddThemeStyleboxOverride("normal", Box(CardFill, Line, 0, 0));
		card.AddThemeStyleboxOverride("hover", Box(CardFill.Lightened(0.08f), Bronze, 0, 0));
		card.AddThemeStyleboxOverride("pressed", selected);
		card.AddThemeStyleboxOverride("hover_pressed", selected);
		card.AddThemeStyleboxOverride("focus", FocusBox(1));
	}

	private static StyleBoxFlat FocusBox(int width)
	{
		StyleBoxFlat focus = Box(Colors.Transparent, StsColors.gold, 0, 0);
		focus.DrawCenter = false;
		focus.SetBorderWidthAll(width);
		return focus;
	}

	public static StyleBoxFlat WalletBox()
	{
		StyleBoxFlat style = Box(SelectedFill, Bronze, 9, 4);
		style.SetCornerRadiusAll(14);
		return style;
	}

	public static StyleBoxFlat EntryBadgeBox()
	{
		StyleBoxFlat style = Box(SelectedFill, Bronze, 3, 0);
		style.SetCornerRadiusAll(7);
		return style;
	}

	public static StyleBoxFlat AmountFieldBox() => Box(FieldFill, ChipBorder, 6, 4);

	public static StyleBoxFlat PaletteBox() => Box(Ink, Bronze, 8, 7);

	public static StyleBoxFlat ToastBox()
	{
		StyleBoxFlat style = Box(Ink, Bronze, 0, 0);
		style.SetCornerRadiusAll(8);
		style.ShadowColor = new Color(0f, 0f, 0f, 0.42f);
		style.ShadowSize = 5;
		style.ShadowOffset = new Vector2(0, 3);
		return style;
	}

	public static StyleBoxFlat Solid(Color color)
	{
		StyleBoxFlat style = Box(color, color, 0, 0);
		style.SetBorderWidthAll(0);
		style.SetCornerRadiusAll(2);
		return style;
	}

	public static void BindPress(Button button, Action action)
	{
		button.Pressed += action;
		button.TreeExiting += () => button.Pressed -= action;
	}

	public static void KeepInView(Control panel, Vector2 desired)
	{
		if (!GodotObject.IsInstanceValid(panel) || !panel.IsInsideTree()) return;
		// Compare the panel with its viewport in the same canvas coordinates.
		Rect2 visible = panel.GetCanvasTransform().AffineInverse() * panel.GetViewportRect();
		Vector2 size = panel.Size.Max(panel.GetCombinedMinimumSize());
		panel.GlobalPosition = new Vector2(
			Mathf.Clamp(desired.X, visible.Position.X + 8, Mathf.Max(visible.Position.X + 8, visible.End.X - size.X - 8)),
			Mathf.Clamp(desired.Y, visible.Position.Y + 8, Mathf.Max(visible.Position.Y + 8, visible.End.Y - size.Y - 8)));
	}
}
