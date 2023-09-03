#if UNITY_WEBGL && !UNITY_EDITOR
#define WEB_MODE
#endif

using System.Runtime.Remoting.Channels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spectrum
{

	public class AudioScaler : MonoBehaviour
	{
		[SerializeField]
		private Transform target;

		[SerializeField]
		private float minScale;
		[SerializeField]
		private float maxScale;
		private float[] loudnessBuffer = new float[256];

		private void Update()
		{
			var Loudness = GetRMS(256, 0);
			var scale = Mathf.Lerp(minScale, maxScale, Loudness);
			gameObject.transform.localScale = new Vector3(scale, scale, scale);
		}

		/// <summary>
		/// Returns the current output volume of the scene's AudioListener, using the RMS method.
		/// </summary>
		/// <param name="sampleSize">The number of samples to take, as a power of two. Higher values mean more precise volume.</param>
		/// <param name="channelUsed">The audio channel to take data from.</param>
		public static float GetRMS(int sampleSize, int channelUsed = 0)
		{
#if WEB_MODE
        return SSWebInteract.GetLoudness();
#else
			sampleSize = Mathf.ClosestPowerOfTwo(sampleSize);
			float[] outputSamples = new float[sampleSize];
			AudioListener.GetOutputData(outputSamples, channelUsed);

			float rms = 0;
			foreach (float f in outputSamples)
			{
				rms += f * f; //sum of squares
			}
			return Mathf.Sqrt(rms / (outputSamples.Length)); //mean and root
#endif
		}
	}
}
