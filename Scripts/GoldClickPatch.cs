using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace GoldLend.Scripts;

[HarmonyPatch(typeof(NTopBarGold), "_Ready")]
internal static class NTopBarGoldReadyPatch
{
	private static readonly Vector2 BadgeSize = new(24, 18);
	private const int BadgeFontSize = 14;

	[HarmonyPostfix]
	private static void Postfix(NTopBarGold __instance)
	{
		if (!GodotObject.IsInstanceValid(__instance)) return;
		GoldUiAnchor.Capture(__instance);
		RunManager? runs = RunManager.Instance;
		string originalTooltip = __instance.TooltipText;
		string? modTooltip = null;
		string symbol = Loc.T("entry.symbol");
		StyleBoxFlat badge = GoldLendUi.EntryBadgeBox();

		void DrawHint()
		{
			if (!GodotObject.IsInstanceValid(__instance) || modTooltip == null) return;
			var rect = new Rect2(new Vector2(__instance.Size.X - BadgeSize.X - 2, 4), BadgeSize);
			Font font = __instance.GetThemeFont("font", "Label");
			Vector2 textSize = font.GetStringSize(symbol, fontSize: BadgeFontSize);
			Vector2 baseline = rect.Position + new Vector2(
				(rect.Size.X - textSize.X) / 2,
				(rect.Size.Y - font.GetHeight(BadgeFontSize)) / 2 + font.GetAscent(BadgeFontSize));
			__instance.DrawStyleBox(badge, rect);
			__instance.DrawString(font, baseline, symbol, fontSize: BadgeFontSize, modulate: StsColors.gold);
		}

		void Redraw()
		{
			if (GodotObject.IsInstanceValid(__instance)) __instance.QueueRedraw();
		}

		void RestoreTooltip()
		{
			if (modTooltip != null && __instance.TooltipText == modTooltip) __instance.TooltipText = originalTooltip;
			modTooltip = null;
		}

		void Refresh()
		{
			if (!GodotObject.IsInstanceValid(__instance)) return;
			if (GoldUiAnchor.TryGetShopPlayer(out _))
			{
				modTooltip = Loc.T("entry.tooltip");
				__instance.TooltipText = modTooltip;
			}
			else
			{
				RestoreTooltip();
				LendPanel.CloseFor(__instance);
			}
			Redraw();
		}

		void OnRunStarted(RunState _) => Refresh();
		void OpenWorkbench(NClickableControl button)
		{
			if (GodotObject.IsInstanceValid(__instance) && GoldUiAnchor.TryGetShopPlayer(out _))
				LendPanel.Toggle(__instance);
		}
		__instance.Released += OpenWorkbench;
		__instance.Draw += DrawHint;
		__instance.Resized += Redraw;
		if (runs != null)
		{
			runs.RunStarted += OnRunStarted;
			runs.RoomEntered += Refresh;
			runs.RoomExited += Refresh;
		}
		__instance.TreeExiting += () =>
		{
			if (runs != null)
			{
				runs.RunStarted -= OnRunStarted;
				runs.RoomEntered -= Refresh;
				runs.RoomExited -= Refresh;
			}
			if (GodotObject.IsInstanceValid(__instance))
			{
				__instance.Released -= OpenWorkbench;
				__instance.Draw -= DrawHint;
				__instance.Resized -= Redraw;
				RestoreTooltip();
			}
			LendPanel.CloseFor(__instance);
			GoldUiAnchor.Release(__instance);
		};
		Refresh();
	}
}
