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
			using (var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
			{
				((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = true;

				yield return request.SendWebRequest();

				if (request.result != UnityWebRequest.Result.Success)
				{
					yield break;
				}

				var dlHandler = (DownloadHandlerAudioClip)request.downloadHandler;
				if (dlHandler.isDone)
				{
					Clip = dlHandler.audioClip;
				}
			}
		}
	}
}
