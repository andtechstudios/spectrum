using System;
using System.Collections;
using System.Security.Policy;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace App
{

	[Serializable]
	public class Config
	{
		public string title;
		public string artist;
	}

	public class Program : MonoBehaviour
	{
		[SerializeField]
		public AudioClip clipOverride;
		[SerializeField]
		private AudioSource audioSource;
		[SerializeField]
		[Range(6f, 13f)]
		private int samplesLengthSize = 6;
		[SerializeField]
		private FFTWindow window;

		public Vector2 scale;

		public int bandCount;

		public Bar[] bars;

		public float energyDecay;
		public float barDecay;
		public float logScale;

		[Header("UI")]
		[SerializeField]
		private TMP_Text songInfoText;
		[SerializeField]
		private RectTransform logoTransform;
		[SerializeField]
		private RectTransform barsTransform;
		[SerializeField]
		private Bar barPrefab;
		[SerializeField]
		[Range(200f, 800f)]
		private int minimumBarLength;
		[SerializeField]
		[Range(400f, 1000f)]
		private int maximumBarLength;

		[SerializeField]
		private AnimationCurve spectrumScaling;
		[SerializeField]
		private Gradient barColors;

		// Storage
		private float[] spectrum;
		private float frameEnergy;
		private Config config;
		private float[] bands;
		private float energy;

		private void Awake()
		{
			int n = 1 << samplesLengthSize;

			spectrum = new float[n];
			bands = new float[bandCount];
			bars = new Bar[bandCount];
		}

		private IEnumerator Start()
		{
			yield return DoConfig();

			songInfoText.text = $"{config.artist}\n\"{config.title}\"\nSpooky Tune Jam 2023";

			yield return DoAudio();
			yield return DoUI();
		}

		IEnumerator DoConfig()
		{
#if UNITY_EDITOR
			var uriPrefix = "file://" + Application.dataPath + "/static/";
#else
			var uriPrefix = Application.absoluteURL;
			uriPrefix = Regex.Replace(uriPrefix, @"/index.html?$", "/");
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
			uriPrefix = Regex.Replace(uriPrefix, @"/index.html?$", "/");
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
			for (int i = 0; i < bands.Length; i++)
			{
				var bar = GameObject.Instantiate(barPrefab, barsTransform);
				bar.transform.localEulerAngles = new Vector3(0f, 0f, -(float)i / bands.Length * 360f);
				bars[i] = bar;
			}

			yield break;
		}

		private void FixedUpdate()
		{
			if (audioSource.clip)
			{
				audioSource.GetSpectrumData(spectrum, 0, window);

				var candidateBands = new float[spectrum.Length];
				frameEnergy = 0f;
				for (int i = 0; i < bands.Length; i++)
				{
					var bucket = Mathf.Clamp(i, 0, bandCount - 1);
					candidateBands[bucket] = spectrum[i];
					frameEnergy = spectrum[i] * spectrum[i];
				}
				frameEnergy = Mathf.Sqrt(frameEnergy) / 0.015f;
				energy = Mathf.Max(frameEnergy, energy);
				energy = Mathf.Clamp01(energy);
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
				energy = Mathf.Max(energy - Time.deltaTime * energyDecay, 0f);
				// Decay since last frame
				for (int i = 0; i < bands.Length; i++)
				{
					bands[i] = Mathf.Max(bands[i] - Time.deltaTime * barDecay, 0f);
				}

				// Update
				for (int i = 0; i < bands.Length; i++)
				{
					var a = bands[i];

					bars[i].transform.localScale = new Vector3(1f, Mathf.Lerp(minimumBarLength, maximumBarLength, a), 1f);
					bars[i].TargetGraphic.color = barColors.Evaluate(a);
				}

				var scale = Mathf.Lerp(0.95f, 1f, energy * energy);
				logoTransform.localScale = new Vector3(scale, scale, scale);
			}
		}
	}
}
