using System.CodeDom;
using System.Collections;
using System.Drawing.Text;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using TreeEditor;
using UnityEngine;

namespace App
{

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

		private float[] spectrum;
		public int bandCount;

		public float[] bands;
		public Bar[] bars;

		public float decay;
		public float logScale;

		[Header("UI")]
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

		private void Awake()
		{
			int n = 1 << samplesLengthSize;
			
			spectrum = new float[n];
			bands = new float[bandCount];
			bars = new Bar[bandCount];
		}

		private IEnumerator Start()
		{
			yield return DoAudio();
			yield return DoUI();
		}

		IEnumerator DoAudio()
		{
			var reader = new AudioReader();

#if UNITY_EDITOR
			var uriPrefix = "file://" + Application.dataPath + "/Audio/";
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

		private void Update()
		{
			if (audioSource.clip)
			{
				audioSource.GetSpectrumData(spectrum, 0, window);
				
				var max = -1f;
				for (int i = 1; i < spectrum.Length - 1; i++)
				{
					
					Debug.DrawLine(new Vector3(scale.x * (i - 1), spectrum[i] + 10, 0), new Vector3(scale.x * i, spectrum[i + 1] + 10, 0), Color.red);
					Debug.DrawLine(new Vector3(scale.x * (i - 1), Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(scale.x * i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
					Debug.DrawLine(new Vector3(scale.x * Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(scale.x * Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
					Debug.DrawLine(new Vector3(scale.x * Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(scale.x * Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
					max = Mathf.Max(max, spectrum[i]);
				}

				//var sampleRange = spectrum.Length / 4;
				//var width = sampleRange / bandCount;
				//for (int i = 0; i < sampleRange; i++)
				//{
				//	var bandIndex = Mathf.Clamp(Mathf.RoundToInt(i / width), 0, bandCount - 1);
				//	candidateBands[bandIndex] += spectrum[i] / max;
				//}

				//Debug.Log(max);
				var candidateBands = new float[spectrum.Length];
				for (int i = 0; i < bands.Length; i++)
				{
					var bucket = Mathf.Clamp(i, 0, bandCount - 1);
					candidateBands[bucket] = spectrum[i];
				}
				for (int i = 0; i < bands.Length; i++)
				{
					float logAmplitude = Mathf.Log10(1 + candidateBands[i]) * logScale;
					logAmplitude = Mathf.Sqrt(logAmplitude);

					var t = (float)i / bandCount;
					logAmplitude *= spectrumScaling.Evaluate(t);

					bands[i] = Mathf.Max(logAmplitude, bands[i]);
					bands[i] = Mathf.Clamp01(bands[i]);
				}

				// Update
				for (int i = 0; i < bands.Length; i++)
				{
					var a = bands[i];
					var t = (float)i / bands.Length;

					bars[i].transform.localScale = new Vector3(1f, Mathf.Lerp(minimumBarLength, maximumBarLength, a), 1f);
					bars[i].TargetGraphic.color = barColors.Evaluate(a);
				}
				for (int i = 0; i < bands.Length; i++)
				{
					bands[i] = Mathf.Max(bands[i] - Time.deltaTime * decay, 0f);
				}
			}
		}
	}
}
