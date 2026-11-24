using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;

namespace GoldLend.Scripts;

public sealed class GoldLendSettings
{
	public bool UseCharacterColors { get; set; } = true;
	public bool ShowPlayerNumbers { get; set; } = true;
	public string ArrivalSound { get; set; } = GoldLendSettingsPage.Off;
}

internal static class GoldLendSettingsPage
{
	internal const string Off = "off";
	internal const string Sound1 = "sound1";
	internal const string Sound2 = "sound2";
	private const string DataKey = "settings";
	private const SaveScope Scope = SaveScope.Global;

	private static readonly ModSettingsValueBinding<GoldLendSettings, bool> CharacterColors = new(
		Entry.ModId, DataKey, Scope, settings => settings.UseCharacterColors,
		(settings, value) => settings.UseCharacterColors = value);

	private static readonly ModSettingsValueBinding<GoldLendSettings, bool> PlayerNumbers = new(
		Entry.ModId, DataKey, Scope, settings => settings.ShowPlayerNumbers,
		(settings, value) => settings.ShowPlayerNumbers = value);

	private static readonly ModSettingsValueBinding<GoldLendSettings, string> ArrivalSound = new(
		Entry.ModId, DataKey, Scope, settings => settings.ArrivalSound,
		(settings, value) => settings.ArrivalSound = value);

	internal static bool UseCharacterColors => CharacterColors.Read();
	internal static bool ShowPlayerNumbers => PlayerNumbers.Read();
	internal static string SelectedArrivalSound => ArrivalSound.Read();

	internal static void Register()
	{
		using (RitsuLibFramework.BeginModDataRegistration(Entry.ModId))
		{
			RitsuLibFramework.GetDataStore(Entry.ModId).Register(
				key: DataKey, fileName: "settings.json", scope: Scope, syncToCloud: false,
				defaultFactory: () => new GoldLendSettings(), autoCreateIfMissing: true);
		}

		RitsuLibFramework.RegisterModSettings(Entry.ModId, page => page
			.WithTitle(Text("panel.title"))
			.WithModDisplayName(Text("panel.title"))
			.AddSection("general", section => section
				.WithTitle(Text("settings.general"))
				.AddToggle("character_colors", Text("settings.characterColors"), CharacterColors)
				.AddToggle("player_numbers", Text("settings.playerNumbers"), PlayerNumbers)
				.AddChoice("arrival_sound", Text("settings.arrivalSound"), ArrivalSound,
					[
						new(Off, Text("settings.soundOff")),
						new(Sound1, Text("settings.sound1")),
						new(Sound2, Text("settings.sound2"))
					],
					presentation: ModSettingsChoicePresentation.Dropdown)));
	}

	private static ModSettingsText Text(string key) => ModSettingsText.Dynamic(() => Loc.T(key));
}
