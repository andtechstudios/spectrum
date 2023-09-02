using System.Collections;
using System.Drawing.Text;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
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

	public class Program : MonoBehaviour
	{
		[SerializeField]
		private AudioSource audioSource;

		private IEnumerator Start()
		{
			var reader = new AudioReader();

#if UNITY_EDITOR
			var uriPrefix = "file://" + Application.dataPath + "/Audio/";
#else
			var uriPrefix = Application.absoluteURL;
			uriPrefix = Regex.Replace(uriPrefix, @"/index.html?$", "/");
#endif

			yield return reader.Read(uriPrefix + "audio.flac", AudioType.UNKNOWN);
			if (!reader.Clip)
			{
				yield return reader.Read(uriPrefix + "audio.wav", AudioType.WAV);
			}
			if (!reader.Clip)
			{
				yield return reader.Read(uriPrefix + "audio.mp3", AudioType.MPEG);
			}

			if (reader.Clip)
			{
				audioSource.clip = reader.Clip;
				audioSource.Play();
			}
			else
			{
				Debug.LogError("Couldn't locate audio file: audio.flac, audio.wav, audio.mp3");
			}
		}
	}
}
