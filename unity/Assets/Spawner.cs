using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEngine.UI.GridLayoutGroup;

namespace App
{

	public class Spawner : MonoBehaviour
	{
		public Box prefab;
		[Range(0f, 1f)]
		public float minSpeed;
		[Range(0f, 1f)]
		public float maxSpeed;
		public float minAngularSpeed;
		public float maxAngularSpeed;
		public int count;
		public TMP_Text scoreText;

		public RectTransform spawnArea;
		public RectTransform viewport;

		private ObjectPool<Box> boxPool;

		public AudioSource coinAudioSource;

		public void Start()
		{
			boxPool = new ObjectPool<Box>(createFunc: CreateBox, actionOnDestroy: DestroyBox, actionOnGet: OnGet, actionOnRelease: OnRelease);

			StartCoroutine(Running());
		}

		void OnGet(Box box)
		{
			box.gameObject.SetActive(true);
		}

		void OnRelease(Box box)
		{
			box.gameObject.SetActive(false);
		}

		Box CreateBox()
		{
			return GameObject.Instantiate(prefab, viewport);
		}

		void DestroyBox(Box box)
		{
			GameObject.Destroy(box.gameObject);
		}

		IEnumerator Running()
		{
			var corners = new Vector3[4];
		
			while (enabled)
			{
				var delay = UnityEngine.Random.Range(0.2f, 1.2f);

				yield return new WaitForSecondsRealtime(delay);

				if (Program.Instance.HadAcquiredFocus)
				{
					var count = Random.Range(1, 4);
					for (int i = 0; i < count; i++)
					{
						Spawn();
					}

					void Spawn()
					{
						spawnArea.GetWorldCorners(corners);
						var min = corners[0];
						var max = corners[2];
						var pos = new Vector2()
						{
							x = Random.Range(min.x, max.x),
							y = Random.Range(min.y, max.y),
						};

						var box = boxPool.Get();
						box.OnCollected += Box_OnCollected;
						box.OnDied += Box_OnDied;

						box.transform.position = new Vector3(pos.x, pos.y, 0f);
						box.transform.localScale = Random.Range(0.5f, 1f) * Vector3.one;
						box.speed = UnityEngine.Random.Range(minSpeed, maxSpeed) * Screen.height;
						box.angularSpeed = Random.Range(maxAngularSpeed, minAngularSpeed);
						box.Activate();
					}
				}
			}
		}

		private void Box_OnDied(object sender, System.EventArgs e)
		{
			var box = sender as Box;
			Unlink(box);
			box.Deactivate();
			boxPool.Release(box);
		}

		void Unlink(Box box)
		{
			box.OnDied -= Box_OnDied;
			box.OnCollected -= Box_OnCollected;
		}

		private void Box_OnCollected(object sender, System.EventArgs e)
		{
			var box = sender as Box;
			Unlink(box);
			box.Deactivate();
			boxPool.Release(box);

			count++;
			scoreText.text = count.ToString();

			coinAudioSource.Play();
		}
	}
}
