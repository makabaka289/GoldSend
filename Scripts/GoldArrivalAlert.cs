using System;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace GoldLend.Scripts;

internal static class GoldArrivalAlert
{
	private static AudioStreamPlayer? _sound1Player;

	internal static void Notify(Player recipient, int previousGold)
	{
		if (recipient.Gold <= previousGold || !LocalContext.IsMe(recipient)) return;
		try
		{
			switch (GoldLendSettingsPage.SelectedArrivalSound)
			{
				case GoldLendSettingsPage.Sound1:
					if (!NonInteractiveMode.IsActive)
						Callable.From(PlaySound1).CallDeferred();
					break;
				case GoldLendSettingsPage.Sound2:
					GoldArrivalVoicePlayer.Play(recipient.Gold - previousGold);
					break;
			}
		}
		catch (Exception exception)
		{
			Entry.Logger.Warn($"GoldLend could not play the arrival alert: {exception.Message}");
		}
	}

	private static void PlaySound1()
	{
		try
		{
			if (NGame.Instance is not { } game || GoldLendSettingsPage.SelectedArrivalSound != GoldLendSettingsPage.Sound1) return;

			SaveManager save = SaveManager.Instance;
			float volume = save.SettingsSave.VolumeMaster * save.SettingsSave.VolumeSfx;
			if (volume <= 0f || (save.PrefsSave.MuteInBackground && !DisplayServer.WindowIsFocused())) return;

			if (_sound1Player == null || !GodotObject.IsInstanceValid(_sound1Player))
			{
				using Stream resource = typeof(GoldArrivalAlert).Assembly.GetManifestResourceStream("GoldLend.ArrivalSound.1A.wav")
					?? throw new InvalidOperationException("GoldLend sound 1 resource is missing.");
				using var buffer = new MemoryStream();
				resource.CopyTo(buffer);
				AudioStreamWav clip = AudioStreamWav.LoadFromBuffer(buffer.ToArray())
					?? throw new InvalidOperationException("GoldLend could not load sound 1.");
				clip.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;

				var player = new AudioStreamPlayer { Name = "GoldLendArrivalSound1", Stream = clip };
				game.AddChild(player);
				_sound1Player = player;
			}

			_sound1Player.VolumeDb = Mathf.LinearToDb(volume * volume);
			_sound1Player.Play();
		}
		catch (Exception exception)
		{
			Entry.Logger.Warn($"GoldLend could not play sound 1: {exception.Message}");
		}
	}
}
