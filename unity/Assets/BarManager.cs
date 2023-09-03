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
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

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
	public int barAmount = 32;
	/// <summary>
	/// Stretches the values of the bars.
	/// </summary>
	[Tooltip("Stretches the values of the bars.")]
	public float barYScale = 50;
	/// <summary>
	/// Sets a minimum scale for the bars; they will never go below this scale.
	/// This value is also used when isEnabled is false.
	/// </summary>
	[Tooltip("Sets a minimum scale for the bars.")]
	public float barMinYScale = 0.1f;
	/// <summary>
	/// The prefab of bar to use when building.
	/// Refer to the documentation to use a custom prefab.
	/// </summary>
	[Tooltip("The prefab of bar to use when building. Choose one from SimpleSpectrum/Bar Prefabs, or refer to the documentation to use a custom prefab.")]
	public Bar barPrefab;
	public Transform barsFolder;
	/// <summary>
	/// Stretches the bars sideways. 
	/// </summary>
	[Tooltip("Stretches the bars sideways.")]
	public float barXScale = 1;
	/// <summary>
	/// The amount of dampening used when the new scale is higher than the bar's existing scale. Must be between 0 (slowest) and 1 (fastest).
	/// </summary>
	[Range(0, 1)]
	[Tooltip("The amount of dampening used when the new scale is higher than the bar's existing scale.")]
	public float attackDamp = 0.3f;
	/// <summary>
	/// The amount of dampening used when the new scale is lower than the bar's existing scale. Must be between 0 (slowest) and 1 (fastest).
	/// </summary>
	[Range(0, 1)]
	[Tooltip("The amount of dampening used when the new scale is lower than the bar's existing scale.")]
	public float decayDamp = 0.15f;
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

	/// <summary>
	/// Returns the output float array used for bar scaling (i.e. after logarithmic scaling and attack/decay). The size of the array depends on barAmount.
	/// </summary>
	public float[] spectrumOutputData
	{
		get
		{
			return oldYScales;
		}
	}


	float[] spectrum;

	//float lograithmicAmplitudePower = 2, multiplyByFrequencyPower = 1.5f;
	Bar[] bars;
	float[] oldYScales; //also optimisation
	float[] oldColorValues; //...optimisation

	float highestLogFreq, frequencyScaleFactor; //multiplier to ensure that the frequencies stretch to the highest record in the array.

	void Start()
	{
		RebuildSpectrum();
	}

	/// <summary>
	/// Rebuilds this instance of Spectrum, applying any changes.
	/// </summary>
	public void RebuildSpectrum()
	{
		//clear all the existing children
		int childs = transform.childCount;
		for (int i = 0; i < childs; i++)
		{
			Destroy(transform.GetChild(i).gameObject);
		}

		numSamples = Mathf.ClosestPowerOfTwo(numSamples);

#if WEB_MODE
        numSamples = SSWebInteract.SetFFTSize(numSamples);
#endif

		//initialise arrays
		spectrum = new float[numSamples];
		bars = new Bar[barAmount];
		oldYScales = new float[barAmount];
		oldColorValues = new float[barAmount];

		//bar instantiation loop
		for (int i = 0; i < barAmount; i++)
		{
			var bar = Instantiate(barPrefab, barsFolder); //create the bars and assign the parent
			bar.transform.localEulerAngles = new Vector3(0f, 0f, -(float)i / barAmount * 360f);

			bars[i] = bar;
		}

		highestLogFreq = Mathf.Log(barAmount + 1, 2); //gets the highest possible logged frequency, used to calculate which sample of the spectrum to use for a bar
		frequencyScaleFactor = 1.0f / (AudioSettings.outputSampleRate / 2) * numSamples;
	}

	void GetSpectrumData(float[] samples, int channel, FFTWindow window)
	{

#if WEB_MODE
      SSWebInteract.GetSpectrumData(samples); //get the spectrum data from the JS lib
#else
		AudioListener.GetSpectrumData(samples, channel, window);
#endif
	}

	void Update()
	{
		GetSpectrumData(spectrum, sampleChannel, windowUsed);

#if WEB_MODE
      float freqLim = frequencyLimitHigh * 0.76f; //AnalyserNode.getFloatFrequencyData doesn't fill the array, for some reason
#else
		float freqLim = frequencyLimitHigh;
#endif

		for (int i = 0; i < bars.Length; i++)
		{
			Bar bar = bars[i];

			float value;
			float trueSampleIndex;

			//GET SAMPLES
			if (useLogarithmicFrequency)
			{
				//LOGARITHMIC FREQUENCY SAMPLING

				//trueSampleIndex = highFrequencyTrim * (highestLogFreq - Mathf.Log(barAmount + 1 - i, 2)) * logFreqMultiplier; //old version

				trueSampleIndex = Mathf.Lerp(frequencyLimitLow, freqLim, (highestLogFreq - Mathf.Log(barAmount + 1 - i, 2)) / highestLogFreq) * frequencyScaleFactor;

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

				trueSampleIndex = Mathf.Lerp(frequencyLimitLow, freqLim, ((float)i) / barAmount) * frequencyScaleFactor;
				//sooooo this one's gotten fancier...
				//firstly a lerp is used between frequency limits to get the 'desired frequency', then it's divided by the outputSampleRate (/2, who knows why) to get its location in the array, then multiplied by numSamples to get an index instead of a fraction.

			}

			//the true sample is usually a decimal, so we need to lerp between the floor and ceiling of it.

			int sampleIndexFloor = Mathf.FloorToInt(trueSampleIndex);
			sampleIndexFloor = Mathf.Clamp(sampleIndexFloor, 0, spectrum.Length - 2); //just keeping it within the spectrum array's range

			value = Mathf.SmoothStep(spectrum[sampleIndexFloor], spectrum[sampleIndexFloor + 1], trueSampleIndex - sampleIndexFloor); //smoothly interpolate between the two samples using the true index's decimal.

			//MANIPULATE & APPLY SAMPLES
			if (multiplyByFrequency) //multiplies the amplitude by the true sample index
			{
#if WEB_MODE
            value = value * (Mathf.Log(trueSampleIndex + 1) + 1);  //different due to how the WebAudioAPI outputs spectrum data.

#else
				value = value * (trueSampleIndex + 1);
#endif
			}
#if !WEB_MODE
			value = Mathf.Sqrt(value); //compress the amplitude values by sqrt(x)
#endif

			//DAMPENING
			//Vector3 oldScale = bar.localScale;
			float oldYScale = oldYScales[i], newYScale;
			if (value * barYScale > oldYScale)
			{
				newYScale = Mathf.Lerp(oldYScale, Mathf.Max(value * barYScale, barMinYScale), attackDamp);
			}
			else
			{
				newYScale = Mathf.Lerp(oldYScale, Mathf.Max(value * barYScale, barMinYScale), decayDamp);
			}

			bar.transform.localScale = new Vector3(barXScale, newYScale, 1);
			bar.TargetGraphic.color = Color.Lerp(colorMin, colorMax, value);

			oldYScales[i] = newYScale;

			//set colour
			Debug.Log("no coloring");
		}
	}

	/// <summary>
	/// Returns a logarithmically scaled and proportionate array of spectrum data from the AudioListener.
	/// </summary>
	/// <param name="spectrumSize">The size of the returned array.</param>
	/// <param name="sampleSize">The size of sample to take from the AudioListener. Must be a power of two. Will only be used in WebGL if no samples have been taken yet.</param>
	/// <param name="windowUsed">The FFTWindow to use when sampling. Unused in WebGL.</param>
	/// <param name="channelUsed">The audio channel to use when sampling. Unused in WebGL.</param>
	/// <returns>A logarithmically scaled and proportionate array of spectrum data from the AudioListener.</returns>
	public static float[] GetLogarithmicSpectrumData(int spectrumSize, int sampleSize, FFTWindow windowUsed = FFTWindow.BlackmanHarris, int channelUsed = 0)
	{
#if WEB_MODE
        sampleSize = SSWebInteract.SetFFTSize(sampleSize); //set the WebGL sampleSize if not already done, otherwise get the current sample size.
#endif
		float[] spectrum = new float[spectrumSize];

		channelUsed = Mathf.Clamp(channelUsed, 0, 1);

		float[] samples = new float[Mathf.ClosestPowerOfTwo(sampleSize)];

#if WEB_MODE
        SSWebInteract.GetSpectrumData(samples); //get the spectrum data from the JS lib
#else
		AudioListener.GetSpectrumData(samples, channelUsed, windowUsed);
#endif

		float highestLogSampleFreq = Mathf.Log(spectrum.Length + 1, 2); //gets the highest possible logged frequency, used to calculate which sample of the spectrum to use for a bar

		float logSampleFreqMultiplier = sampleSize / highestLogSampleFreq;

		for (int i = 0; i < spectrum.Length; i++) //for each float in the output
		{

			float trueSampleIndex = (highestLogSampleFreq - Mathf.Log(spectrum.Length + 1 - i, 2)) * logSampleFreqMultiplier; //gets the index equiv of the logified frequency

			//the true sample is usually a decimal, so we need to lerp between the floor and ceiling of it.

			int sampleIndexFloor = Mathf.FloorToInt(trueSampleIndex);
			sampleIndexFloor = Mathf.Clamp(sampleIndexFloor, 0, samples.Length - 2); //just keeping it within the spectrum array's range

			float value = Mathf.SmoothStep(spectrum[sampleIndexFloor], spectrum[sampleIndexFloor + 1], trueSampleIndex - sampleIndexFloor); //smoothly interpolate between the two samples using the true index's decimal.

#if WEB_MODE
            value = value * (Mathf.Log(trueSampleIndex + 1) + 1); //different due to how the WebAudioAPI outputs spectrum data.

#else
			value = value * (trueSampleIndex + 1); //multiply value by its position to make it proportionate
			value = Mathf.Sqrt(value); //compress the amplitude values by sqrt(x)
#endif
			spectrum[i] = value;
		}
		return spectrum;
	}
}
