using UnityEngine;
using UnityEngine.UI;

namespace Spectrum
{
	public class Bar : MonoBehaviour
	{
		public Graphic TargetGraphic => targetGraphic;
		[SerializeField]
		private Graphic targetGraphic;
	}
}
