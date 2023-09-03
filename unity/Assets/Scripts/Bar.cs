using UnityEngine;
using UnityEngine.UI;

namespace App
{
	public class Bar : MonoBehaviour
	{
		public Graphic TargetGraphic => targetGraphic;
		[SerializeField]
		private Graphic targetGraphic;
	}
}
