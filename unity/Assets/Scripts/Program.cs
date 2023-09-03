using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Security.Policy;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace App
{

	public enum SampleChannel
	{
		PingPong = -1,
		Left = 0,
		Right = 1,
	}

	public class Program : MonoBehaviour
	{
		public static Program Instance { get; private set; }

		public BarManager barManager;

		[Header("Program Settings")]
		[SerializeField]
		public AudioClip clipOverride;
		public AudioSource AudioSource => audioSource;
		[SerializeField]
		private AudioSource audioSource;
		[SerializeField]
		private AudioListener audioListener;

		public int SamplingCount => 1 << samplingSizeFactor;
		[Header("Spectrum Settings")]
		[SerializeField]
		[Range(6f, 12f)]
		private int samplingSizeFactor = 10;
		public int BandCount => bandCount;
		[SerializeField]
		[Range(32, 100)]
		private int bandCount = 64;
		public FFTWindow SamplingWindow => samplingWindow;
		[SerializeField]
		private FFTWindow samplingWindow = FFTWindow.Rectangular;
		public SampleChannel SamplingChannel => samplingChannel;
		[SerializeField]
		private SampleChannel samplingChannel = SampleChannel.Left;

		[Header("UI Settings")]
		[SerializeField]
		private TMP_Text songInfoText;
		[SerializeField]
		private RectTransform logoTransform;
		public SpectrumWidget spectrumWidget;

		// Storage
		private Config config;
		private float[] samples = new float[256];

		private void OnEnable()
		{
			Instance = Instance ? Instance : this;
		}

		private void OnDisable()
		{
			Instance = Instance == this ? null : Instance;
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

		IEnumerator FadeIn()
		{
			var delay = 2f;
			var fadeDuration = 3f;

			// Wait for browser to allow audio
			do
			{
				if (SimpleSpectrumApi.GetLoudness(samples, 0) > Mathf.Epsilon)
				{
					break;
				}

				yield return new WaitForSeconds(1f);
			}
			while (true);

			// Delay, then fade in
			yield return new WaitForSeconds(delay);
			for (float t = 0; t < fadeDuration; t += Time.deltaTime)
			{
				yield return null;

				songInfoText.color = new Color(1f, 1f, 1f, t / fadeDuration);
			}
			songInfoText.color = Color.white;
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
