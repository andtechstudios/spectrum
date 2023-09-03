using UnityEngine;

namespace App
{

	public class SpectrumWidget : MonoBehaviour
	{
		[SerializeField]
		private RectTransform barsTransform;
		[SerializeField]
		private Bar barPrefab;
		[SerializeField]
		[Range(200f, 600f)]
		private int minimumBarLength;
		[SerializeField]
		[Range(500f, 800f)]
		private int maximumBarLength;

		[SerializeField]
		private Gradient barColors;
		private Bar[] bars;

		public void Init(int count)
		{
			bars = new Bar[count];

			for (int i = 0; i < count; i++)
			{
				var bar = GameObject.Instantiate(barPrefab, barsTransform);
				bar.transform.localEulerAngles = new Vector3(0f, 0f, -(float)i / count * 360f);
				bars[i] = bar;
			}
		}
	}
}
