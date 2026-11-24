using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;

namespace GoldLend.Scripts;

[ModInitializer("Init")]
public static class Entry
{
	public const string ModId = "GoldLend";

	public static readonly Logger Logger = RitsuLibFramework.CreateLogger("GoldLend", LogType.Generic);

	public static void Init()
	{
		Loc.Initialize();
		GoldLendSettingsPage.Register();
		LendNetAction.Register();
		GoldRequestMessages.Subscribe();
		new Harmony("sts2.goldlend").PatchAll();
		Logger.Info("GoldLend initialized.");
	}
}
