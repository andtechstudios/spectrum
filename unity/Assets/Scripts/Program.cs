using System;
using System.Collections;
using Spectrum;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Program : MonoBehaviour
{
	public static Program Instance { get; private set; }

	public bool HadAcquiredFocus { get; private set; } = false;
	public bool HadPerceivedAudio { get; private set; } = false;
	public float Loudness { get; private set; }

	[Header("Program Settings")]
	[SerializeField]
	public AudioClip clipOverride;

	public int NumSamples => 1 << numSamplesFactor;
	[Header("Sampling Settings")]
	[SerializeField]
	[Range(6f, 12f)]
	private int numSamplesFactor = 10;
	public int NumBands => numBands;
	[SerializeField]
	[Range(32, 100)]
	private int numBands = 64;
	public SampleChannel SamplingChannel => samplingChannel;
	[SerializeField]
	private SampleChannel samplingChannel = SampleChannel.Left;
	public FFTWindow SamplingWindow => samplingWindow;
	[SerializeField]
	private FFTWindow samplingWindow = FFTWindow.Rectangular;
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

	public AudioSource AudioSource => audioSource;
	[Header("Components")]
	[SerializeField]
	private AudioSource audioSource;
	[SerializeField]
	private BarManager barManager;
	[SerializeField]
	private Graphic playGraphic;
	[SerializeField]
	private TMP_Text chryonText;

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

		PresetUI();
		barManager.Init(sampler);

		yield return LoadConfigAsync();

		var text = string.Empty;
		text = string.IsNullOrEmpty(config.artist) ? text : text + $"{config.artist}";
		text = string.IsNullOrEmpty(config.title) ? text : text + $"\n\"{config.title}\"";
		text += "\nSpooky Tune Jam 2023";
		chryonText.text = text;

		yield return LoadAudioAsync();

		StartCoroutine(FadeInChryonAsync());
	}

	void PresetUI()
	{
		chryonText.color = Color.clear;
	}
	IEnumerator FadeInChryonAsync()
	{
		var delay = 2f;
		var fadeDuration = 3f;

		// Wait for browser to allow audio
		do
		{
			if (HadPerceivedAudio)
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

			chryonText.color = new Color(1f, 1f, 1f, t / fadeDuration);
		}
		chryonText.color = Color.white;
	}

	IEnumerator LoadConfigAsync()
	{
#if UNITY_EDITOR
		var uriPrefix = "file://" + Application.dataPath + "/WebGLTemplates/Spectrum/";
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

	IEnumerator LoadAudioAsync()
	{
		var reader = new AudioClipReader();

#if UNITY_EDITOR
		var uriPrefix = "file://" + Application.dataPath + "/WebGLTemplates/Spectrum/";
#else
			var uriPrefix = Application.absoluteURL;
			uriPrefix = System.Text.RegularExpressions.Regex.Replace(uriPrefix, @"/index.html?$", "/");
#endif

		var clip = clipOverride;
		if (!clip)
		{
			yield return reader.DownloadAsync(uriPrefix + "audio.mp3", AudioType.MPEG);
			clip = reader.Clip;
		}

		if (clip)
		{
			audioSource.clip = clip;
		}
		else
		{
			Debug.LogError("Audio file not found. You must upload 'audio.mp3' along with Spectrum.");
		}
	}

	void FixedUpdate()
	{
		if (HadAcquiredFocus)
		{
			Loudness = Mathf.Max(SimpleSpectrumApi.GetLoudness(loudnessBuffer, 0), Loudness);
			HadPerceivedAudio = HadPerceivedAudio || Loudness > 0.05f;
			sampler?.OnFixedUpdate();
		}
	}
	void Update()
	{
		Loudness = Mathf.Max(Loudness - Time.deltaTime * loudnessDecaySpeed, 0f);

		if (!HadAcquiredFocus)
		{
			sampler.StepIdleBars();
		}

		sampler?.OnUpdate();
	}

	public void RequestAcquireFocus()
	{
		if (HadAcquiredFocus)
		{
			return;
		}

		HadAcquiredFocus = true;
		playGraphic.enabled = false;
		audioSource.Play();
	}
}