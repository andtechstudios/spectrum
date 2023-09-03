using System;
using System.Collections;
using System.Security.Policy;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace App
{

	public class Program : MonoBehaviour
	{
		public static Program Instance { get; private set; }

		[Header("Program Settings")]
		[SerializeField]
		public AudioClip clipOverride;
		public AudioSource AudioSource => audioSource;
		[SerializeField]
		private AudioSource audioSource;
		[SerializeField]
		private AudioListener audioListener;

		public int SampleSize => 1 << samplesLengthSize;
		[Header("Spectrum Settings")]
		[SerializeField]
		[Range(6f, 13f)]
		private int samplesLengthSize = 6;
		[Range(32, 100)]
		public int bandCount;
		[SerializeField]
		private FFTWindow window;

		[Header("UI Settings")]
		[SerializeField]
		private TMP_Text songInfoText;
		[SerializeField]
		private RectTransform logoTransform;
		public SpectrumWidget spectrumWidget;

		// Storage
		private Config config;

		private void OnEnable()
		{
			Instance = Instance ? Instance : this;
		}

		private void OnDisable()
		{
			Instance = Instance == this ? null : Instance;
		}

		IEnumerator FadeIn()
		{
			var delay = 2f;
			var fadeDuration = 3f;

			yield return new WaitForSeconds(delay);
			for (float t = 0; t < fadeDuration; t += Time.deltaTime)
			{
				yield return null;

				songInfoText.color = new Color(1f, 1f, 1f, t / fadeDuration);
			}
			songInfoText.color = Color.white;
		}

		private IEnumerator Start()
		{
			songInfoText.color = Color.clear;

			yield return DoConfig();

			songInfoText.text = $"{config.artist}\n\"{config.title}\"\nSpooky Tune Jam 2023";

			yield return DoAudio();

			StartCoroutine(FadeIn());

			//yield return DoUI();
		}

		IEnumerator DoConfig()
		{
#if UNITY_EDITOR
			var uriPrefix = "file://" + Application.dataPath + "/static/";
#else
			var uriPrefix = Application.absoluteURL;
			uriPrefix = System.Text.RegularExpressions.Regex.Replace(uriPrefix, @"/index.html?$", "/");
#endif
			var uri = uriPrefix + "config.json";
			using (var request = UnityWebRequest.Get(uri))
			{
				yield return request.SendWebRequest();

				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(request.error);
				}
				else
				{
					config = JsonUtility.FromJson<Config>(request.downloadHandler.text);
				}
			}
		}

		IEnumerator DoAudio()
		{
			var reader = new AudioReader();

#if UNITY_EDITOR
			var uriPrefix = "file://" + Application.dataPath + "/static/";
#else
			var uriPrefix = Application.absoluteURL;
			uriPrefix = System.Text.RegularExpressions.Regex.Replace(uriPrefix, @"/index.html?$", "/");
#endif

			var clip = clipOverride;
			if (!clip)
			{
				yield return reader.Read(uriPrefix + "audio.flac", AudioType.UNKNOWN);
				clip = reader.Clip;
			}
			if (!clip)
			{
				yield return reader.Read(uriPrefix + "audio.wav", AudioType.WAV);
				clip = reader.Clip;
			}
			if (!clip)
			{
				yield return reader.Read(uriPrefix + "audio.mp3", AudioType.MPEG);
				clip = reader.Clip;
			}

			if (clip)
			{
				audioSource.clip = clip;
				audioSource.Play();
			}
			else
			{
				Debug.LogError("Couldn't locate audio file: audio.flac, audio.wav, audio.mp3");
			}
		}

		IEnumerator DoUI()
		{
			spectrumWidget.Init(bandCount);

			yield break;
		}
	}
}
