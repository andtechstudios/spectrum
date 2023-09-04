using UnityEngine;

namespace Spectrum
{

	public class PlaybackController : MonoBehaviour
	{

		private void Update()
		{
			if (Program.Instance && Program.Instance.AudioSource.clip)
			{
				var duration = Program.Instance.AudioSource.clip.length;
				var audioSource = Program.Instance.AudioSource;

				if (Input.GetKeyDown(KeyCode.LeftArrow))
				{
					audioSource.time = Mathf.Clamp(audioSource.time - 5f, 0f, duration - 0.01f);
				}
				if (Input.GetKeyDown(KeyCode.RightArrow))
				{
					audioSource.time = Mathf.Clamp(audioSource.time + 5f, 0f, duration - 0.01f);
				}
				if (Input.GetKeyDown(KeyCode.J))
				{
					audioSource.time = Mathf.Clamp(audioSource.time - 10f, 0f, duration - 0.01f);
				}
				if (Input.GetKeyDown(KeyCode.L))
				{
					audioSource.time = Mathf.Clamp(audioSource.time + 10f, 0f, duration - 0.01f);
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
