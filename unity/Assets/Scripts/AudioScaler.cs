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
			var Loudness = SimpleSpectrumApi.GetLoudness(loudnessBuffer, 0);
			var scale = Mathf.Lerp(minScale, maxScale, Loudness);
			gameObject.transform.localScale = new Vector3(scale, scale, scale);
		}
	}
}
