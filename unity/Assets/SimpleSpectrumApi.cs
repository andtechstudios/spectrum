#if !UNITY_WEBGL
#define MICROPHONE_AVAILABLE
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
#define WEB_MODE //different to UNITY_WEBGL, as we still want functionality in the Editor!
#endif

using UnityEngine;

namespace App
{
	public static class SimpleSpectrumApi
	{

		public static float GetLoudness(float[] samples, int channel)
		{
#if WEB_MODE
        return SSWebInteract.GetLoudness();
#else
			AudioListener.GetOutputData(samples, channel);

			float rms = 0;
			for (int i = 0; i < samples.Length; i++)
			{
				var f = samples[i];
				rms += f * f; //sum of squares
			}
			return Mathf.Sqrt(rms / (samples.Length)); //mean and root
#endif
		}

			public static void GetSpectrumData(float[] samples, int channel, FFTWindow window)
		{

#if WEB_MODE
      SSWebInteract.GetSpectrumData(samples); //get the spectrum data from the JS lib
#else
			AudioListener.GetSpectrumData(samples, channel, window);
#endif
		}

		/// <summary>
		/// Returns a logarithmically scaled and proportionate array of spectrum data from the AudioListener.
		/// </summary>
		/// <param name="spectrumSize">The size of the returned array.</param>
		/// <param name="sampleSize">The size of sample to take from the AudioListener. Must be a power of two. Will only be used in WebGL if no samples have been taken yet.</param>
		/// <param name="windowUsed">The FFTWindow to use when sampling. Unused in WebGL.</param>
		/// <param name="channel">The audio channel to use when sampling. Unused in WebGL.</param>
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
}
