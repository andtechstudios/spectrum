using System.Runtime.Remoting.Channels;
using UnityEngine;
using UnityEngine.UIElements;

namespace App
{

	public class AudioScaler : MonoBehaviour
	{
		[SerializeField]
		private Transform target;

		[SerializeField]
		private float minScale;
		[SerializeField]
		private float maxScale;
		[SerializeField]
		private float decaySpeed;

		private float[] samples = new float[256];
		private float energy;

		private void FixedUpdate()
		{
			var energy = SimpleSpectrumApi.GetLoudness(samples, 0);
			this.energy = Mathf.Max(energy, this.energy);
		}

		private void Update()
		{
			// Decay
			energy = Mathf.Max(energy - Time.deltaTime * decaySpeed, 0f);

			// Apply
			var scale = Mathf.Lerp(minScale, maxScale, energy);
			gameObject.transform.localScale = new Vector3(scale, scale, scale);
		}
	}
}
