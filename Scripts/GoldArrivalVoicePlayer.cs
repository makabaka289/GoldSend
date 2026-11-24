using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace GoldLend.Scripts;

internal static class GoldArrivalVoicePlayer
{
	private static readonly Queue<int> Pending = new();
	private static readonly Dictionary<string, AudioStreamMP3> Clips = new();
	private static AudioStreamPlayer? _player;

	internal static void Play(int amount)
	{
		if (!NonInteractiveMode.IsActive)
			Callable.From(() => Enqueue(amount)).CallDeferred();
	}

	private static void Enqueue(int amount)
	{
		try
		{
			if (NGame.Instance is not { } game) return;
			if (_player == null || !GodotObject.IsInstanceValid(_player))
			{
				Pending.Clear();
				_player = new AudioStreamPlayer { Name = "GoldLendArrivalVoice" };
				game.AddChild(_player);
				_player.Finished += PlayNext;
			}

			Pending.Enqueue(amount);
			if (!_player.Playing) PlayNext();
		}
		catch (Exception exception)
		{
			Entry.Logger.Warn($"GoldLend could not queue arrival voice: {exception.Message}");
		}
	}

	private static void PlayNext()
	{
		try
		{
			if (_player == null || !GodotObject.IsInstanceValid(_player))
			{
				Pending.Clear();
				return;
			}
			_player.Stream = null;
			if (Pending.Count == 0) return;

			SaveManager save = SaveManager.Instance;
			SettingsSave settings = save.SettingsSave;
			float volume = settings.VolumeMaster * settings.VolumeSfx;
			if (GoldLendSettingsPage.SelectedArrivalSound != GoldLendSettingsPage.Sound2 || volume <= 0f
				|| (save.PrefsSave.MuteInBackground && !DisplayServer.WindowIsFocused()))
			{
				Pending.Clear();
				return;
			}

			string[] tokens = GoldArrivalVoiceClips.Tokenize(Pending.Dequeue());
			var playlist = new AudioStreamPlaylist
			{
				StreamCount = tokens.Length,
				FadeTime = 0f,
				Loop = false
			};
			for (int index = 0; index < tokens.Length; index++)
				playlist.SetListStream(index, GetClip(tokens[index]));
			_player.Stream = playlist;
			_player.VolumeDb = Mathf.LinearToDb(volume * volume);
			_player.Play();
		}
		catch (Exception exception)
		{
			Pending.Clear();
			Entry.Logger.Warn($"GoldLend could not play arrival voice: {exception.Message}");
		}
	}

	private static AudioStreamMP3 GetClip(string token)
	{
		if (Clips.TryGetValue(token, out AudioStreamMP3? clip)) return clip;
		using Stream resource = typeof(GoldArrivalVoicePlayer).Assembly.GetManifestResourceStream($"GoldLend.ArrivalVoice.{token}.mp3")
			?? throw new InvalidOperationException($"GoldLend arrival voice clip is missing: {token}");
		using var audio = new MemoryStream();
		resource.CopyTo(audio);
		clip = AudioStreamMP3.LoadFromBuffer(audio.ToArray())
			?? throw new InvalidOperationException($"GoldLend arrival voice clip is invalid: {token}");
		Clips.Add(token, clip);
		return clip;
	}
}
