using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace App
{
	public class AudioReader
	{
		public AudioClip Clip { get; private set; }

		public IEnumerator Read(string uri, AudioType audioType = AudioType.MPEG)
		{
			Clip = null;
			using (var uwr = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
			{
				((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;

				yield return uwr.SendWebRequest();

				if (uwr.result != UnityWebRequest.Result.Success)
				{
					yield break;
				}

				var dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
				if (dlHandler.isDone)
				{
					Clip = dlHandler.audioClip;
				}
			}
		}
	}
}
