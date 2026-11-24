using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GoldLend.Tests")]

namespace GoldLend.Scripts;

internal static class GoldAmountMath
{
	public const int Maximum = 99999;

	public static int StepSize(bool shift, bool control) => control ? 1 : shift ? 50 : 10;

	public static int Add(int amount, int delta) => (int)Math.Clamp((long)amount + delta, 0, Maximum);

	public static int Clear() => 0;

	public static int All(int gold) => Math.Max(0, gold);

	public static bool TryParse(string text, out int amount)
	{
		amount = 0;
		foreach (char character in text)
		{
			if (character is < '0' or > '9')
			{
				amount = 0;
				return false;
			}
			amount = Math.Min(amount * 10 + (character - '0'), Maximum);
		}
		return true;
	}

	public static bool CanRequest(int amount) => amount > 0;

	public static bool CanLend(int amount, int gold) => CanRequest(amount) && amount <= gold;

	public static int? Remaining(int amount, int gold) => amount <= gold ? gold - amount : null;

	public static int RequestPayment(int requested, int gold) => Math.Max(0, Math.Min(requested, gold));
}
