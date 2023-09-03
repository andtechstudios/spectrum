using System.Runtime.Remoting.Channels;
using UnityEngine;

namespace App
{

	public class Program2 : MonoBehaviour
	{
		public float loudness;
		public GameObject gameObject;
		public int sampleAmount = 256;

		private void Start()
		{
			sampleAmount = SSWebInteract.SetFFTSize(sampleAmount);
		}

		private void FixedUpdate()
		{
			var energy = SSWebInteract.GetLoudness();
			gameObject.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 2f, energy);
		}
	}
}
