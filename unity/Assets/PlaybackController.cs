using UnityEngine;

namespace App
{

	public class PlaybackController : MonoBehaviour
	{

		private void Update()
		{
			if (Program.Instance)
			{
				var duration = Program.Instance.AudioSource.clip.length;
				var audioSource = Program.Instance.AudioSource;

				if (Input.GetKeyDown(KeyCode.LeftArrow))
				{
					audioSource.time = Mathf.Clamp(audioSource.time - 5f, 0f, duration);
				}
				if (Input.GetKeyDown(KeyCode.RightArrow))
				{
					audioSource.time = Mathf.Clamp(audioSource.time + 5f, 0f, duration);
				}

				for (int i = 0; i < 10; i++)
				{
					var digit = (i + 10 - 1) % 10;
					var keycode = KeyCode.Alpha0 + i;
					var percent = (float)digit / 10;

					if (Input.GetKeyDown(keycode))
					{
						audioSource.time = duration * percent;
					}
				}
			}
		}
	}
}
