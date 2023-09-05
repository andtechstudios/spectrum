using UnityEngine;
using UnityEngine.UI;

namespace Spectrum
{
	public class Bar : MonoBehaviour
	{
		public Color Color
		{
			set
			{
				barGraphic.color = value;
			}
		}

		[SerializeField]
		private Graphic barGraphic;
	}
}
