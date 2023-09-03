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
		[Header("Program Settings")]
		[SerializeField]
		public AudioClip clipOverride;
		[SerializeField]
		private AudioSource audioSource;
		[SerializeField]
		private AudioListener audioListener;

		[Header("Spectrum Settings")]
		[SerializeField]
		[Range(6f, 13f)]
		private int samplesLengthSize = 6;
		[Range(32, 100)]
		public int bandCount;
		[SerializeField]
		private FFTWindow window;
		[SerializeField]
		private float energyDecay;
		[SerializeField]
		private float barDecay;
		[SerializeField]
		private float logScale;

		[Header("UI Settings")]
		[SerializeField]
		private TMP_Text songInfoText;
		[SerializeField]
		private RectTransform logoTransform;
		public SpectrumWidget spectrumWidget;

		[SerializeField]
		private AnimationCurve spectrumScaling;

		// Storage
		private Config config;
		private float energy;
		private float[] spectrum;
		private float[] bands;

		private IEnumerator Start()
		{
			int n = 1 << samplesLengthSize;
			spectrum = new float[n];
			bands = new float[bandCount];

			yield return DoConfig();

			songInfoText.text = $"{config.artist}\n\"{config.title}\"\nSpooky Tune Jam 2023";

			yield return DoAudio();
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

		private void FixedUpdate()
		{
			if (audioSource.clip)
			{
				AudioListener.GetSpectrumData(spectrum, 0, window);

				var candidateBands = new float[spectrum.Length];
				var energy = 0f;
				for (int i = 0; i < bands.Length; i++)
				{
					var bucket = Mathf.Clamp(i, 0, bandCount - 1);
					candidateBands[bucket] = spectrum[i];
					energy = spectrum[i] * spectrum[i];
				}
				energy = Mathf.Sqrt(energy) / 0.015f;
				this.energy = Mathf.Max(energy, this.energy);
				this.energy = Mathf.Clamp01(this.energy);
				for (int i = 0; i < bands.Length; i++)
				{
					float logAmplitude = Mathf.Log10(1 + candidateBands[i]) * logScale;
					logAmplitude = Mathf.Sqrt(logAmplitude);

					var t = (float)i / bandCount;
					logAmplitude *= spectrumScaling.Evaluate(t);

					bands[i] = Mathf.Max(logAmplitude, bands[i]);
					bands[i] = Mathf.Clamp01(bands[i]);
				}
			}
		}

		private void Update()
		{
			if (audioSource.clip)
			{
				// Decay since last frame
				energy = Mathf.Max(energy - Time.deltaTime * energyDecay, 0f);
				for (int i = 0; i < bands.Length; i++)
				{
					bands[i] = Mathf.Max(bands[i] - Time.deltaTime * barDecay, 0f);
				}
			}
		}
	}
}
