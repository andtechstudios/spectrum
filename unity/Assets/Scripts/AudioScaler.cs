using UnityEngine;

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

		private void Update()
		{
			if (Program.Instance)
			{
				var Loudness = Program.Instance.Loudness;
				var scale = Mathf.Lerp(minScale, maxScale, Loudness);
				gameObject.transform.localScale = new Vector3(scale, scale, scale);
			}
		}
	}
}
