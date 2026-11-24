using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Runs;

namespace GoldLend.Scripts;

internal static class PlayerGoldBadge
{
	private const int Auto = 0;
	private static readonly (string Key, Color Color)[] ColorsById =
	[
		("color.auto", StsColors.cream),
		("color.red", new Color("FF8383")),
		("color.green", new Color("9FD976")),
		("color.orange", new Color("FFB46C")),
		("color.blue", StsColors.blue),
		("color.yellow", StsColors.yellow),
		("color.purple", new Color("D6A2F1")),
		("color.cyan", new Color("77DCD2")),
		("color.cream", StsColors.cream)
	];

	private sealed class Marks
	{
		public Dictionary<ulong, int> Choices { get; } = new();
		public event Action<ulong>? Changed;

		public void Set(ulong netId, int choice)
		{
			if (choice == Auto) Choices.Remove(netId);
			else Choices[netId] = choice;
			Changed?.Invoke(netId);
		}
	}

	private static readonly ConditionalWeakTable<IRunState, Marks> RunMarks = new();

	public static void AddIdentity(HBoxContainer row, IRunState run, ulong netId, Player? player, Label name, Label number,
		Control? card = null, Action<Color?>? markChanged = null)
	{
		Marks marks = RunMarks.GetValue(run, static _ => new Marks());
		bool useCharacterColors = GoldLendSettingsPage.UseCharacterColors;
		Color defaultNameColor = useCharacterColors ? CharacterColor(player?.Character) : StsColors.cream;
		Color defaultNumberColor = useCharacterColors ? defaultNameColor : StsColors.gold;
		string identifier = netId.ToString();
		for (int slot = 0; slot < run.Players.Count; slot++)
		{
			if (run.Players[slot].NetId == netId)
			{
				identifier = Loc.T("player.slot", slot + 1);
				break;
			}
		}

		var badge = new Button
		{
			Text = identifier,
			Visible = GoldLendSettingsPage.ShowPlayerNumbers,
			TooltipText = Loc.T("player.mark.tooltip", netId),
			MouseDefaultCursorShape = Control.CursorShape.PointingHand,
			ButtonMask = MouseButtonMask.Left | MouseButtonMask.Right,
			ActionMode = BaseButton.ActionModeEnum.Release,
			CustomMinimumSize = card == null ? new Vector2(48, 36) : new Vector2(38, 24)
		};
		if (card != null) badge.AddThemeFontSizeOverride("font_size", 13);
		row.AddChild(badge);

		var palette = new PopupMenu();
		palette.AddThemeStyleboxOverride("panel", GoldLendUi.PaletteBox());
		palette.AddThemeColorOverride("font_color", StsColors.cream);
		palette.AddThemeFontSizeOverride("font_size", 18);
		row.AddChild(palette);
		for (int id = 0; id < ColorsById.Length; id++)
		{
			var option = ColorsById[id];
			using Image swatch = Image.CreateEmpty(14, 14, false, Image.Format.Rgba8);
			swatch.Fill(id == Auto ? defaultNameColor : option.Color);
			string key = id == Auto && !useCharacterColors ? "color.reset" : option.Key;
			palette.AddIconRadioCheckItem(ImageTexture.CreateFromImage(swatch), Loc.T(key), id);
		}

		void Refresh()
		{
			if (!GodotObject.IsInstanceValid(badge) || !GodotObject.IsInstanceValid(palette)) return;
			int selected = marks.Choices.GetValueOrDefault(netId, Auto);
			Color nameColor = selected == Auto ? defaultNameColor : ColorsById[selected].Color;
			Color numberColor = selected == Auto ? defaultNumberColor : nameColor;
			Color? accent = useCharacterColors || selected != Auto ? nameColor : null;
			GoldLendUi.StyleButton(badge, nameColor, accent: accent);
			if (GodotObject.IsInstanceValid(name)) name.AddThemeColorOverride("font_color", nameColor);
			if (GodotObject.IsInstanceValid(number)) number.AddThemeColorOverride("font_color", numberColor);
			for (int i = 0; i < palette.ItemCount; i++) palette.SetItemChecked(i, palette.GetItemId(i) == selected);
			markChanged?.Invoke(accent);
		}

		void OnMarkChanged(ulong changedId)
		{
			if (changedId == netId) Refresh();
		}

		void ChooseMark(long id) => marks.Set(netId, (int)id);
		palette.IdPressed += ChooseMark;
		palette.TreeExiting += () => palette.IdPressed -= ChooseMark;

		void OpenPalette(Control anchor)
		{
			if (!GodotObject.IsInstanceValid(palette) || !GodotObject.IsInstanceValid(anchor)) return;
			if (palette.Visible)
			{
				palette.Hide();
				return;
			}
			Vector2 origin = anchor.GetGlobalTransformWithCanvas() * new Vector2(0, anchor.Size.Y + 4);
			palette.PopupOnParent(new Rect2I((Vector2I)origin, Vector2I.Zero));
		}
		GoldLendUi.BindPress(badge, () => OpenPalette(badge));
		Control trigger = card ?? name;
		if (card == null)
		{
			name.MouseFilter = Control.MouseFilterEnum.Stop;
			name.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
			name.TooltipText = badge.TooltipText;
		}
		void OpenWithRightClick(InputEvent input)
		{
			if (!GodotObject.IsInstanceValid(trigger)
				|| input is not InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }) return;
			OpenPalette(trigger);
			trigger.AcceptEvent();
		}
		trigger.GuiInput += OpenWithRightClick;
		trigger.TreeExiting += () => trigger.GuiInput -= OpenWithRightClick;
		marks.Changed += OnMarkChanged;
		badge.TreeExiting += () => marks.Changed -= OnMarkChanged;
		Refresh();
	}

	public static void BindBalance(Player player, Label number)
	{
		void Refresh()
		{
			if (GodotObject.IsInstanceValid(number)) number.Text = player.Gold.ToString();
		}
		player.GoldChanged += Refresh;
		number.TreeExiting += () => player.GoldChanged -= Refresh;
		Refresh();
	}

	private static Color CharacterColor(CharacterModel? character) => character switch
	{
		Ironclad => ColorsById[1].Color,
		Silent => ColorsById[2].Color,
		Regent => ColorsById[3].Color,
		Defect => ColorsById[4].Color,
		Fool => ColorsById[5].Color,
		Necrobinder => ColorsById[6].Color,
		_ => StsColors.cream
	};
}
