#if UNITY_WEBGL && !UNITY_EDITOR
#define WEB_MODE //different to UNITY_WEBGL, as we still want functionality in the Editor!
#endif

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Spectrum
{

	public class Program : MonoBehaviour
	{
		public static Program Instance { get; private set; }

		public bool HasAcquiredFocus { get; private set; } = false;
		public float Loudness { get; private set; }

		[Header("Program Settings")]
		[SerializeField]
		public AudioClip clipOverride;
		public AudioSource AudioSource => audioSource;
		[SerializeField]
		private AudioSource audioSource;
		[SerializeField]
		private BarManager barManager;
		[SerializeField]
		private bool autoPlay;

		public int NumSamples => 1 << numSamplesFactor;
		[Header("Sampling Settings")]
		[SerializeField]
		[Range(6f, 12f)]
		private int numSamplesFactor = 10;
		public int NumBands => numBands;
		[SerializeField]
		[Range(32, 100)]
		private int numBands = 64;
		public FFTWindow SamplingWindow => samplingWindow;
		[SerializeField]
		private FFTWindow samplingWindow = FFTWindow.Rectangular;
		public SampleChannel SamplingChannel => samplingChannel;
		[SerializeField]
		private SampleChannel samplingChannel = SampleChannel.Left;
		[Space]
		[Tooltip("If true, audio data is scaled logarithmically.")]
		public bool useLogarithmicFrequency = true;
		[Tooltip("If true, the values of the spectrum are multiplied based on their frequency, to keep the values proportionate.")]
		public bool multiplyByFrequency = true;
		[Tooltip("The lower bound of the freuqnecy range to sample from. Leave at 0 when unused.")]
		public float frequencyLimitLow = 10;
		[Tooltip("The upper bound of the freuqnecy range to sample from. Leave at 22050 (44100/2) when unused.")]
		public float frequencyLimitHigh = 11025;
		[Tooltip("How quickly loudness decay over time.")]
		public float loudnessDecaySpeed = 2;
		[Tooltip("How quickly bands decay over time.")]
		public float bandDecaySpeed = 4;

		[Header("UI Settings")]
		[SerializeField]
		private TMP_Text songInfoText;
		[SerializeField]
		private RectTransform logoTransform;
		[SerializeField]
		private Graphic playGraphic;

		// Storage
		private Config config;
		private Sampler sampler;
		private float[] loudnessBuffer = new float[256];

		private void OnEnable()
		{
			Instance = Instance ? Instance : this;
		}
		private void OnDisable()
		{
			Instance = Instance == this ? null : Instance;
		}

		public void RequestAcquireFocus()
		{
			if (HasAcquiredFocus)
			{
				return;
			}

			HasAcquiredFocus = true;
			audioSource.Play();
		}

		private IEnumerator Start()
		{
			sampler = new Sampler(NumSamples, numBands)
			{
				DecaySpeed = bandDecaySpeed,
				FrequencyLimitHigh = frequencyLimitHigh,
				FrequencyLimitLow = frequencyLimitLow,
				MultiplyByFrequency = multiplyByFrequency,
				UseLogarithmicFrequency = useLogarithmicFrequency,
			};

#if WEB_MODE
			HasAcquiredFocus = false;
#else
			HasAcquiredFocus = autoPlay;
#endif
			PresetUI();

			yield return DoConfig();

			songInfoText.text = $"{config.artist}\n\"{config.title}\"\nSpooky Tune Jam 2023";

			yield return DoAudio();
			barManager.Init(sampler);
			StartCoroutine(FadeInChryon());

		}

		private void FixedUpdate()
		{
			Loudness = Mathf.Max(SimpleSpectrumApi.GetLoudness(loudnessBuffer, 0), Loudness);
			HasAcquiredFocus = HasAcquiredFocus || Loudness > 0.05f;

			sampler?.OnFixedUpdate();

			playGraphic.enabled = !HasAcquiredFocus && (Time.realtimeSinceStartup > 0.666f || !autoPlay);
		}
		private void Update()
		{
			Loudness = Mathf.Max(Loudness - Time.deltaTime * loudnessDecaySpeed, 0f);
			sampler?.OnUpdate();
		}

		void PresetUI()
		{
			songInfoText.color = Color.clear;
			playGraphic.enabled = false;
		}
		IEnumerator FadeInChryon()
		{
			var delay = 2f;
			var fadeDuration = 3f;

			// Wait for browser to allow audio
			do
			{
				if (HasAcquiredFocus)
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
				if (autoPlay)
				{
					audioSource.Play();
				}
			}
			else
			{
				Debug.LogError("Couldn't locate audio file: audio.flac, audio.wav, audio.mp3");
			}
		}
	}
}
