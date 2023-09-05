using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Spectrum
{

	public class AudioClipReader
	{
		public AudioClip Clip { get; private set; }

		public IEnumerator DownloadAsync(string uri, AudioType audioType = AudioType.MPEG)
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
