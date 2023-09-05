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

		private void LateUpdate()
		{
			if (Program.Instance)
			{
				var value = Program.Instance.Loudness;
				value *= value;

				var scale = Mathf.Lerp(minScale, maxScale, value);
				gameObject.transform.localScale = new Vector3(scale, scale, scale);
			}
		}
	}
}
