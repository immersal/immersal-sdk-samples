/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Immersal.Samples.Mapping
{
	public class MappingUIComponent : MonoBehaviour
	{
		[SerializeField]
		private GameObject target_button = null;
		[SerializeField]
		private TextMeshProUGUI[] texts = null;

		private Image image = null;
		private Button button = null;

		private Color button_normalColor = new Color(0f, 0f, 0f, 0.8f);
		private Color button_disabledColor = new Color(0f, 0f, 0f, 0.4f);

		private Color icon_normalColor = new Color(1f, 1f, 1f, 1f);
		private Color icon_disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

		private void Start() {
			image = GetComponent<Image>();
			if(image == null) {
				image = target_button.GetComponent<Image>();
			}

			button = GetComponent<Button>();
			if (button == null) {
				button = target_button.GetComponent<Button>();
			}
		}

		public void Activate() {
			if(image != null) {
				image.color = button_normalColor;
			}
			if(button != null) {
				ColorBlock cb = button.colors;
				cb.normalColor = icon_normalColor;
				button.colors = cb;
				button.interactable = true;
			}
			foreach (TextMeshProUGUI text in texts) {
				if (text != null) {
					text.color = icon_normalColor;
				}
			}
		}

		public void Disable() {
			if (image != null) {
				image.color = button_disabledColor;
			}
			if (button != null) {
				ColorBlock cb = button.colors;
				cb.normalColor = icon_disabledColor;
				button.colors = cb;
				button.interactable = false;
			}
			foreach (TextMeshProUGUI text in texts) {
				if (text != null) {
					text.color = icon_disabledColor;
				}
			}
		}
	}
}