using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace App
{
	public class Box : MonoBehaviour, IPointerDownHandler
	{
		public float speed;
		public float angularSpeed;
		public Graphic graphic;

		public event EventHandler OnCollected;
		public event EventHandler OnDied;

		public RectTransform rectTransform => transform as RectTransform;

		private float life;

		public void Activate()
		{
			life = 0f;
			enabled = true;
			graphic.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
		}

		public void Deactivate()
		{
			OnDied = null;
			OnCollected = null;
			enabled = false;
		}

		void Update()
		{
			life += Time.deltaTime;

			if (life > 4f)
			{
				OnDied?.Invoke(this, EventArgs.Empty);
				return;
			}

			transform.Translate(0f, -speed * Time.deltaTime, 0f);
			graphic.transform.Rotate(0f, 0f, angularSpeed * Time.deltaTime, Space.Self);
		}

		void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
		{
			OnCollected?.Invoke(this, EventArgs.Empty);
		}
	}
}
