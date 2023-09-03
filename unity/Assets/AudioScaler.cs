using System.Runtime.Remoting.Channels;
using UnityEngine;

namespace App
{

	public class AudioScaler : MonoBehaviour
	{
		[SerializeField]
		private Transform target;

		[SerializeField]
		float minScale;
		[SerializeField]
		float maxScale;

		private float[] samples = new float[256];

		private void FixedUpdate()
		{
			var energy = SimpleSpectrumApi.GetLoudness(samples, 0);
			var scale = Mathf.Lerp(minScale, maxScale, energy);
			gameObject.transform.localScale = new Vector3(scale, scale, scale);
		}
	}
}
