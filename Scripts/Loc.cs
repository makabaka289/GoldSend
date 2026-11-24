using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;

namespace GoldLend.Scripts;

internal static class Loc
{
	private const string ResourcePrefix = "GoldLend.Localization.";

	private const string DefaultLanguage = "eng";

	private const string ChineseFallback = "zhs";

	private static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		Assembly assembly = Assembly.GetExecutingAssembly();
		string[] names = assembly.GetManifestResourceNames();
		foreach (string name in names)
		{
			if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal) || !name.EndsWith(".json", StringComparison.Ordinal))
			{
				continue;
			}
			string language = name.Substring(ResourcePrefix.Length, name.Length - ResourcePrefix.Length - ".json".Length);
			using Stream? stream = assembly.GetManifestResourceStream(name);
			if (stream == null)
			{
				continue;
			}
			try
			{
				Dictionary<string, string>? table = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
				if (table != null && table.Count > 0)
				{
					Tables[language] = table;
				}
			}
			catch (Exception ex)
			{
				Entry.Logger.Warn("本地化表 " + name + " 解析失败：" + ex.Message);
			}
		}
		Entry.Logger.Info("本地化表已加载：[" + string.Join(", ", Tables.Keys) + "]");
	}

	public static string T(string key, params object[] args)
	{
		Initialize();
		string text = Lookup(CurrentLanguage(), key) ?? Lookup(DefaultLanguage, key) ?? Lookup(ChineseFallback, key) ?? key;
		return (args.Length == 0) ? text : string.Format(text, args);
	}

	private static string? Lookup(string language, string key)
	{
		return (Tables.TryGetValue(language, out Dictionary<string, string>? table) && table.TryGetValue(key, out string? value)) ? value : null;
	}

	private static string CurrentLanguage()
	{
		string? language = LocManager.Instance?.Language;
		if (string.IsNullOrWhiteSpace(language))
		{
			return DefaultLanguage;
		}
		string normalized = language.Trim().ToLowerInvariant();
		if (Tables.ContainsKey(normalized))
		{
			return normalized;
		}
		string primary = normalized.Split('-', '_')[0];
		if (primary.StartsWith("zh", StringComparison.Ordinal))
		{
			return ChineseFallback;
		}
		return Tables.ContainsKey(primary) ? primary : DefaultLanguage;
	}
}
