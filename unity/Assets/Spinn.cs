using UnityEngine;

namespace App
{
	public class Spinn : MonoBehaviour
	{
		public RectTransform icon;
		public float angularSpeed;

		// Start is called before the first frame update
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			icon.Rotate(0f, 0f, angularSpeed * Time.deltaTime);
		}
	}
}
