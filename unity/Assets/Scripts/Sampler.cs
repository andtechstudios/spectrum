/*
SimpleSpectrum.cs - Part of Simple Spectrum V2.1 by Sam Boyer.
*/

#if !UNITY_WEBGL
#define MICROPHONE_AVAILABLE
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
#define WEB_MODE //different to UNITY_WEBGL, as we still want functionality in the Editor!
#endif

using UnityEngine;

namespace Spectrum
{

	public class Sampler
	{
		public float[] Bands { get; private set; }
		public int NumSamples => spectrum.Length;
		public int NumBands => Bands.Length;
		public bool UseLogarithmicFrequency
		{
			get => useLogarithmicFrequency;
			set => useLogarithmicFrequency = value;
		}
		public bool MultiplyByFrequency
		{
			get => multiplyByFrequency;
			set => multiplyByFrequency = value;
		}
		public float FrequencyLimitLow
		{
			get => frequencyLimitLow;
			set => frequencyLimitLow = value;
		}
		public float FrequencyLimitHigh
		{
			get => frequencyLimitHigh;
			set => frequencyLimitHigh = value;
		}
		public float DecaySpeed
		{
			get => decaySpeed;
			set => decaySpeed = value;
		}
		private bool useLogarithmicFrequency = true;
		private bool multiplyByFrequency = true;
		private float frequencyLimitLow = 10f;
		private float frequencyLimitHigh = 11025f;
		private float decaySpeed = 4f;

		float[] spectrum;
		float highestLogFreq, frequencyScaleFactor;

		public Sampler(int numSamples, int numBands)
		{
			numSamples = Mathf.ClosestPowerOfTwo(numSamples);
#if WEB_MODE
      numSamples = SSWebInteract.SetFFTSize(numSamples);
#endif

			//initialise arrays
			spectrum = new float[numSamples];
			Bands = new float[numBands];
			highestLogFreq = Mathf.Log(numBands + 1, 2); //gets the highest possible logged frequency, used to calculate which sample of the spectrum to use for a bar
			frequencyScaleFactor = 1.0f / (AudioSettings.outputSampleRate / 2) * numSamples;
		}

		public void OnFixedUpdate()
		{
			SimpleSpectrumApi.GetSpectrumData(spectrum, (int)Program.Instance.SamplingChannel, Program.Instance.SamplingWindow);

#if WEB_MODE
      float freqLim = frequencyLimitHigh * 0.76f; //AnalyserNode.getFloatFrequencyData doesn't fill the array, for some reason
#else
			float freqLim = FrequencyLimitHigh;
#endif

			for (int i = 0; i < NumBands; i++)
			{
				float sample;
				float trueSampleIndex;

				//GET SAMPLES
				if (UseLogarithmicFrequency)
				{
					//LOGARITHMIC FREQUENCY SAMPLING

					//trueSampleIndex = highFrequencyTrim * (highestLogFreq - Mathf.Log(barAmount + 1 - i, 2)) * logFreqMultiplier; //old version

					trueSampleIndex = Mathf.Lerp(FrequencyLimitLow, freqLim, (highestLogFreq - Mathf.Log(NumBands + 1 - i, 2)) / highestLogFreq) * frequencyScaleFactor;

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

					trueSampleIndex = Mathf.Lerp(FrequencyLimitLow, freqLim, ((float)i) / NumBands) * frequencyScaleFactor;
					//sooooo this one's gotten fancier...
					//firstly a lerp is used between frequency limits to get the 'desired frequency', then it's divided by the outputSampleRate (/2, who knows why) to get its location in the array, then multiplied by numSamples to get an index instead of a fraction.

				}

				//the true sample is usually a decimal, so we need to lerp between the floor and ceiling of it.

				int sampleIndexFloor = Mathf.FloorToInt(trueSampleIndex);
				sampleIndexFloor = Mathf.Clamp(sampleIndexFloor, 0, spectrum.Length - 2); //just keeping it within the spectrum array's range

				sample = Mathf.SmoothStep(spectrum[sampleIndexFloor], spectrum[sampleIndexFloor + 1], trueSampleIndex - sampleIndexFloor); //smoothly interpolate between the two samples using the true index's decimal.

				//MANIPULATE & APPLY SAMPLES
				if (MultiplyByFrequency) //multiplies the amplitude by the true sample index
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

				Bands[i] = Mathf.Max(sample, Bands[i]);
			}
		}

		public void OnUpdate()
		{
			for (int i = 0; i < Bands.Length; i++)
			{
				// Decay
				Bands[i] = Mathf.Max(Bands[i] - Time.deltaTime * decaySpeed, 0f);
			}
		}
	}
}
