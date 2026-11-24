using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace GoldLend.Scripts;

internal static class GoldUiAnchor
{
	private const string NativeGoldIcon = "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_gold.tres";
	private static NTopBarGold? _gold;

	private static Texture2D? _icon;

	public static NTopBarGold? Gold => (_gold != null && GodotObject.IsInstanceValid(_gold)) ? _gold : null;

	public static Texture2D? IconTexture => (_icon != null && GodotObject.IsInstanceValid(_icon)) ? _icon : null;

	public static void Capture(NTopBarGold gold)
	{
		if (!GodotObject.IsInstanceValid(gold)) return;
		_gold = gold;
		if (IconTexture is null)
		{
			_icon = FindIconTexture(gold);
		}
	}

	public static void Release(NTopBarGold gold)
	{
		if (_gold == gold) _gold = null;
	}

	public static bool TryGetShopPlayer(out Player local)
	{
		local = null!;
		RunState? state = RunManager.Instance?.DebugOnlyGetState();
		if (state?.CurrentRoom is not MerchantRoom || state.Players.Count < 2
			|| LocalContext.GetMe(state) is not { } player) return false;
		local = player;
		return true;
	}

	public static Player? LocalPlayer()
	{
		RunState? state = RunManager.Instance?.DebugOnlyGetState();
		return (state == null) ? null : LocalContext.GetMe(state);
	}

	public static Control? ResolveHost(NTopBarGold gold)
	{
		if (!GodotObject.IsInstanceValid(gold) || !gold.IsInsideTree()) return null;
		Node? popupHost = gold.GetNodeOrNull<Node>("%GoldPopup")?.GetParent();
		if (popupHost is Control control && GodotObject.IsInstanceValid(control))
		{
			return control;
		}
		for (Node? node = gold.GetParent(); node != null; node = node.GetParent())
		{
			if (node is Control candidate && GodotObject.IsInstanceValid(candidate) && candidate.Name.ToString() == "TopBar")
			{
				return candidate;
			}
		}
		return null;
	}

	private static Texture2D? FindIconTexture(NTopBarGold gold)
	{
		return (gold.FindChild("GoldIcon", true, false) as TextureRect)?.Texture
			?? (ResourceLoader.Exists(NativeGoldIcon) ? ResourceLoader.Load<Texture2D>(NativeGoldIcon) : null);
	}

	public static string DisplayName(Player player)
	{
		var service = RunManager.Instance?.NetService;
		string? name = service != null ? PlatformUtil.GetPlayerNameRaw(service.Platform, player.NetId) : null;
		return string.IsNullOrWhiteSpace(name) ? Loc.T("player.fallback", player.NetId) : name;
	}
}
