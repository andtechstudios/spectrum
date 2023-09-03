/*
SimpleSpectrum.cs - Part of Simple Spectrum V2.1 by Sam Boyer.
*/

#if !UNITY_WEBGL
#define MICROPHONE_AVAILABLE
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
#define WEB_MODE //different to UNITY_WEBGL, as we still want functionality in the Editor!
#endif

using App;
using UnityEngine;

public class BarManager : MonoBehaviour
{

	#region SAMPLING PROPERTIES
	/// <summary>
	/// The audio channel to use when sampling.
	/// </summary>
	[Tooltip("The audio channel to use when sampling.")]
	public int sampleChannel = 0;
	/// <summary>
	/// The number of samples to use when sampling. Must be a power of two.
	/// </summary>
	[Tooltip("The number of samples to use when sampling. Must be a power of two.")]
	public int numSamples = 256;
	/// <summary>
	/// The FFTWindow to use when sampling.
	/// </summary>
	[Tooltip("The FFTWindow to use when sampling.")]
	public FFTWindow windowUsed = FFTWindow.BlackmanHarris;
	/// <summary>
	/// If true, audio data is scaled logarithmically.
	/// </summary>
	[Tooltip("If true, audio data is scaled logarithmically.")]
	public bool useLogarithmicFrequency = true;
	/// <summary>
	/// If true, the values of the spectrum are multiplied based on their frequency, to keep the values proportionate.
	/// </summary>
	[Tooltip("If true, the values of the spectrum are multiplied based on their frequency, to keep the values proportionate.")]
	public bool multiplyByFrequency = true;

	/// <summary>
	/// The lower bound of the freuqnecy range to sample from. Leave at 0 when unused.
	/// </summary>
	[Tooltip("The lower bound of the freuqnecy range to sample from. Leave at 0 when unused.")]
	public float frequencyLimitLow = 0;

	/// <summary>
	/// The uppwe bound of the freuqnecy range to sample from. Leave at 22050 when unused.
	/// </summary>
	[Tooltip("The upper bound of the freuqnecy range to sample from. Leave at 22050 (44100/2) when unused.")]
	public float frequencyLimitHigh = 22050;

	/*
   /// <summary>
   /// Determines what percentage of the full frequency range to use (1 being the full range, reducing the value towards 0 cuts off high frequencies).
   /// This can be useful when using MP3 files or audio with missing high frequencies.
   /// </summary>
   [Range(0, 1)]
   [Tooltip("Determines what percentage of the full frequency range to use (1 being the full range, reducing the value towards 0 cuts off high frequencies).\nThis can be useful when using MP3 files or audio with missing high frequencies.")]
   public float highFrequencyTrim = 1;
   /// <summary>
   /// When useLogarithmicFrequency is false, this value stretches the spectrum data onto the bars.
   /// </summary>
   [Tooltip("Stretches the spectrum data when mapping onto the bars. A lower value means the spectrum is populated by lower frequencies.")]
   public float linearSampleStretch = 1;
   */
	#endregion

	#region BAR PROPERTIES
	/// <summary>
	/// The amount of bars to use.
	/// </summary>
	[Tooltip("The amount of bars to use. Does not have to be equal to Num Samples, but probably should be lower.")]
	public int bandCount = 32;
	/// <summary>
	/// Stretches the bars sideways. 
	/// </summary>
	[Tooltip("Stretches the bars sideways.")]
	public float barXScale = 1;
	/// <summary>
	/// Sets a minimum scale for the bars; they will never go below this scale.
	/// This value is also used when isEnabled is false.
	/// </summary>
	[Tooltip("Sets a minimum scale for the bars.")]
	public float barMinYScale = 0.1f;
	/// <summary>
	/// Stretches the values of the bars.
	/// </summary>
	[Tooltip("Stretches the values of the bars.")]
	public float barYMaxScale = 50;
	/// <summary>
	/// The prefab of bar to use when building.
	/// Refer to the documentation to use a custom prefab.
	/// </summary>
	[Tooltip("The prefab of bar to use when building. Choose one from SimpleSpectrum/Bar Prefabs, or refer to the documentation to use a custom prefab.")]
	public Bar barPrefab;
	public Transform barsFolder;
	public float decaySpeed;
	#endregion

	#region COLOR PROPERTIES
	/// <summary>
	/// The minimum (low value) color if useColorGradient is true, else the solid color to use.
	/// </summary>
	[Tooltip("The minimum (low value) color if useColorGradient is true, else the solid color to use.")]
	public Color colorMin = Color.black;
	/// <summary>
	/// The maximum (high value) color if useColorGradient is true.
	/// </summary>
	[Tooltip("The maximum (high value) color.")]
	public Color colorMax = Color.white;
	#endregion

	/// <summary>
	/// The raw audio spectrum data. Can be set to custom values if the sourceType is set to Custom.
	/// (For a 1:1 data to bar mapping, set barAmount equal to numSamples, disable useLogarithmicFrequency and set linearSampleStretch to 1)
	/// </summary>
	public float[] spectrumInputData
	{
		get
		{
			return spectrum;
		}
		set
		{
			Debug.LogError("Error from SimpleSpectrum: spectrumInputData cannot be set while sourceType is not Custom.");
		}
	}

	float[] spectrum;
	float[] bands;
	float highestLogFreq, frequencyScaleFactor; //multiplier to ensure that the frequencies stretch to the highest record in the array.
	Bar[] bars;

	void Start()
	{
		RebuildSpectrum();
	}

	/// <summary>
	/// Rebuilds this instance of Spectrum, applying any changes.
	/// </summary>
	public void RebuildSpectrum()
	{
		numSamples = Mathf.ClosestPowerOfTwo(numSamples);
#if WEB_MODE
      numSamples = SSWebInteract.SetFFTSize(numSamples);
#endif

		//initialise arrays
		spectrum = new float[numSamples];
		bands = new float[bandCount];
		highestLogFreq = Mathf.Log(bandCount + 1, 2); //gets the highest possible logged frequency, used to calculate which sample of the spectrum to use for a bar
		frequencyScaleFactor = 1.0f / (AudioSettings.outputSampleRate / 2) * numSamples;
		bars = new Bar[bandCount];

		//bar instantiation loop
		for (int i = 0; i < bandCount; i++)
		{
			var bar = Instantiate(barPrefab, barsFolder); //create the bars and assign the parent
			bar.transform.localEulerAngles = new Vector3(0f, 0f, -(float)i / bandCount * 360f);

			bars[i] = bar;
		}
	}

	private void FixedUpdate()
	{
		OnFixedUpdate();
	}
	private void Update()
	{
		OnUpdate();
	}

	public void OnFixedUpdate()
	{
		SimpleSpectrumApi.GetSpectrumData(spectrum, sampleChannel, windowUsed);

#if WEB_MODE
      float freqLim = frequencyLimitHigh * 0.76f; //AnalyserNode.getFloatFrequencyData doesn't fill the array, for some reason
#else
		float freqLim = frequencyLimitHigh;
#endif

		for (int i = 0; i < bars.Length; i++)
		{
			float sample;
			float trueSampleIndex;

			//GET SAMPLES
			if (useLogarithmicFrequency)
			{
				//LOGARITHMIC FREQUENCY SAMPLING

				//trueSampleIndex = highFrequencyTrim * (highestLogFreq - Mathf.Log(barAmount + 1 - i, 2)) * logFreqMultiplier; //old version

				trueSampleIndex = Mathf.Lerp(frequencyLimitLow, freqLim, (highestLogFreq - Mathf.Log(bandCount + 1 - i, 2)) / highestLogFreq) * frequencyScaleFactor;

				//'logarithmic frequencies' just means we want to bias to the lower frequencies.
				//by doing log2(max(i)) - log2(max(i) - i), we get a flipped log graph
				//(make a graph of log2(64)-log2(64-x) to see what I mean)
				//this isn't finished though, because that graph doesn't actually map the bar index (x) to the spectrum index (y).
				//then we divide by highestLogFreq to make the graph to map 0-barAmount on the x axis to 0-1 in the y axis.
				//we then use this to Lerp between frequency limits, and then an index is calculated.
				//also 1 gets added to barAmount pretty much everywhere, because without it, the log hits (barAmount-1,max(freq))

			}
			else
			{
				//LINEAR (SCALED) FREQUENCY SAMPLING 
				//trueSampleIndex = i * linearSampleStretch; //don't like this anymore

				trueSampleIndex = Mathf.Lerp(frequencyLimitLow, freqLim, ((float)i) / bandCount) * frequencyScaleFactor;
				//sooooo this one's gotten fancier...
				//firstly a lerp is used between frequency limits to get the 'desired frequency', then it's divided by the outputSampleRate (/2, who knows why) to get its location in the array, then multiplied by numSamples to get an index instead of a fraction.

			}

			//the true sample is usually a decimal, so we need to lerp between the floor and ceiling of it.

			int sampleIndexFloor = Mathf.FloorToInt(trueSampleIndex);
			sampleIndexFloor = Mathf.Clamp(sampleIndexFloor, 0, spectrum.Length - 2); //just keeping it within the spectrum array's range

			sample = Mathf.SmoothStep(spectrum[sampleIndexFloor], spectrum[sampleIndexFloor + 1], trueSampleIndex - sampleIndexFloor); //smoothly interpolate between the two samples using the true index's decimal.

			//MANIPULATE & APPLY SAMPLES
			if (multiplyByFrequency) //multiplies the amplitude by the true sample index
			{
#if WEB_MODE
            sample = sample * (Mathf.Log(trueSampleIndex + 1) + 1);  //different due to how the WebAudioAPI outputs spectrum data.

#else
				sample = sample * (trueSampleIndex + 1);
#endif
			}
#if !WEB_MODE
			sample = Mathf.Sqrt(sample); //compress the amplitude values by sqrt(x)
#endif

			//DAMPENING

			bands[i] = Mathf.Max(sample, bands[i]);
		}
	}

	public void OnUpdate()
	{
		if (Program.Instance)
		{
			for (int i = 0; i < bands.Length; i++)
			{
				var bar = bars[i];
				var value = bands[i];

				// Decay
				bands[i] = Mathf.Max(bands[i] - Time.deltaTime * decaySpeed, 0f);

				bar.transform.localScale = new Vector3(barXScale, Mathf.Lerp(barMinYScale, barYMaxScale, value), 1);
				bar.TargetGraphic.color = Color.Lerp(colorMin, colorMax, value);
			}
		}
	}
}
