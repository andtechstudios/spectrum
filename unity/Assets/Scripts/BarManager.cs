using UnityEngine;

namespace Spectrum
{

	public class BarManager : MonoBehaviour
	{
		/// <summary>
		/// The minimum (low value) color if useColorGradient is true, else the solid color to use.
		/// </summary>
		[Tooltip("The minimum (low value) color if useColorGradient is true, else the solid color to use.")]
		public Color colorMin = Color.black;
		/// <summary>
		/// The maximum (high value) color if useColorGradient is true.
		/// </summary>
		[Tooltip("The maximum (high value) color.")]
		public Color colorMax = Color.white;
		[Range(-6f, 5f)]
		public float angularSpeed = 40f;
		[Tooltip("Stretches the bars sideways.")]
		public float xMinScale = 1;
		/// <summary>
		/// Stretches the bars sideways. 
		/// </summary>
		[Tooltip("Stretches the bars sideways.")]
		public float xMaxScale = 1;
		/// <summary>
		/// Sets a minimum scale for the bars; they will never go below this scale.
		/// This value is also used when isEnabled is false.
		/// </summary>
		[Tooltip("Sets a minimum scale for the bars.")]
		public float yMinScale = 0.1f;
		/// <summary>
		/// Stretches the values of the bars.
		/// </summary>
		[Tooltip("Stretches the values of the bars.")]
		public float yMaxScale = 50;

		[Header("Components")]
		public Bar barPrefab;
		public Transform barsFolder;
		/// <summary>
		/// Stretches the bars sideways. 
		/// </summary>

		Sampler sampler;
		Bar[] bars;

		public void Init(Sampler sampler)
		{
			this.sampler = sampler;
			var numBands = sampler.NumBands;

			bars = new Bar[numBands];
			for (int i = 0; i < numBands; i++)
			{
				var bar = Instantiate(barPrefab, barsFolder); //create the bars and assign the parent
				bar.transform.localEulerAngles = new Vector3(0f, 0f, -(float)i / numBands * 360f);

				bars[i] = bar;
			}
		}

		private void LateUpdate()
		{
			if (bars is not null)
			{
				for (int i = 0; i < bars.Length; i++)
				{
					var value = sampler.Bands[i];
					value *= value;

					// Apply
					var bar = bars[i];
					bar.transform.localScale = new Vector3()
					{
						x = Mathf.Lerp(xMinScale, xMaxScale, value),
						y = Mathf.Lerp(yMinScale, yMaxScale, value),
						z = 1f,
					};
					bar.Color = Color.Lerp(colorMin, colorMax, value);
				}
			}

			barsFolder.localEulerAngles = new Vector3(0f, 0f, Time.time * angularSpeed);
		}
	}
}
