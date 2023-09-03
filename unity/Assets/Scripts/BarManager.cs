/*
SimpleSpectrum.cs - Part of Simple Spectrum V2.1 by Sam Boyer.
*/

#if !UNITY_WEBGL
#define MICROPHONE_AVAILABLE
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
#define WEB_MODE //different to UNITY_WEBGL, as we still want functionality in the Editor!
#endif

using Spectrum;
using UnityEngine;

public class BarManager : MonoBehaviour
{
	[Tooltip("The prefab of bar to use when building. Choose one from SimpleSpectrum/Bar Prefabs, or refer to the documentation to use a custom prefab.")]
	public Bar barPrefab;
	public Transform barsFolder;
	/// <summary>
	/// Stretches the bars sideways. 
	/// </summary>
	[Tooltip("Stretches the bars sideways.")]
	public float barXScale = 1;
	/// <summary>
	/// Sets a minimum scale for the bars; they will never go below this scale.
	/// This value is also used when isEnabled is false.
	/// </summary>
	[Tooltip("Sets a minimum scale for the bars.")]
	public float barMinYScale = 0.1f;
	/// <summary>
	/// Stretches the values of the bars.
	/// </summary>
	[Tooltip("Stretches the values of the bars.")]
	public float barYMaxScale = 50;
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

	private void Update()
	{
		if (bars is not null)
		{
			for (int i = 0; i < bars.Length; i++)
			{
				var bar = bars[i];
				var value = sampler.Bands[i];

				// Decay
				bar.transform.localScale = new Vector3(barXScale, Mathf.Lerp(barMinYScale, barYMaxScale, value), 1);
				bar.TargetGraphic.color = Color.Lerp(colorMin, colorMax, value);
			}
		}
	}
}
