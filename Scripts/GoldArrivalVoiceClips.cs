using System.Globalization;

namespace GoldLend.Scripts;

internal static class GoldArrivalVoiceClips
{
	private static readonly (int Divisor, string? Unit)[] Sections =
		[(100_000_000, "tts_ten_million"), (10_000, "tts_ten_thousand"), (1, null)];
	private static readonly (int Divisor, string? Unit)[] Places =
		[(1_000, "tts_thousand"), (100, "tts_hundred"), (10, "tts_ten"), (1, null)];

	internal static string[] Tokenize(int amount)
	{
		if (amount <= 0) return [];

		List<string> tokens = ["diaoluo_da", "tts_success"];
		bool skippedSection = false;
		foreach (var (divisor, unit) in Sections)
		{
			int section = amount / divisor % 10_000;
			bool firstSection = tokens.Count == 2;
			if (section == 0)
			{
				skippedSection |= !firstSection;
				continue;
			}
			if (!firstSection && (skippedSection || section < 1_000)) tokens.Add("tts_0");
			skippedSection = false;
			AppendSection(tokens, section, firstSection);
			if (unit != null) tokens.Add(unit);
		}
		tokens.Add("tts_yuan");
		return tokens.ToArray();
	}

	private static void AppendSection(List<string> tokens, int section, bool firstSection)
	{
		bool pendingZero = false;
		foreach (var (divisor, unit) in Places)
		{
			int digit = section / divisor % 10;
			if (digit == 0)
			{
				pendingZero |= section >= divisor;
				continue;
			}

			if (pendingZero) tokens.Add("tts_0");
			pendingZero = false;
			if (!(firstSection && section < 20 && divisor == 10 && digit == 1))
				tokens.Add("tts_" + digit.ToString(CultureInfo.InvariantCulture));
			if (unit != null) tokens.Add(unit);
		}
	}

}
