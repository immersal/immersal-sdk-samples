/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Immersal.REST;

namespace Immersal.Samples.Mapping.ScrollList
{
	public class ScrollListItem : MonoBehaviour {
		public enum MapState { Queued, Processing, Done, Failed };
		public MapState mapState = MapState.Done;

		private SDKJob m_data = null;

		[SerializeField]
		private VisualizeManager visualizeManager = null;
		[SerializeField]
		private Sprite sprite_queued = null;
		[SerializeField]
		private Sprite sprite_processing = null;
		[SerializeField]
		private Sprite sprite_done = null;
		[SerializeField]
		private Sprite sprite_failed = null;

		[SerializeField]
		private Image iconField = null;
		[SerializeField]
		private TextMeshProUGUI nameField = null;
		[SerializeField]
		private TextMeshProUGUI dateField = null;
		[SerializeField]
		private Toggle toggle = null;
		private bool m_ToggleLock = false;

		public SDKJob data
		{
			get { return m_data; }
			set
			{
				m_data = value;
				switch (m_data.status)
				{
					case "done":
					mapState = MapState.Done; break;
					case "processing":
					mapState = MapState.Processing; break;
					case "failed":
					mapState = MapState.Failed; break;
					case "pending":
					mapState = MapState.Queued; break;
				}

				SetName();
				SetDate();
				SetIcon();
			}
		}

		public void OnClick() {
			toggle.isOn = !toggle.isOn;
		}

		public void OnToggleChanged(bool value)
		{
			if (!m_ToggleLock)
				SelectListItem();
		}

		private void SelectListItem()
		{
			if (visualizeManager != null && m_data.status == "done")
			{
                visualizeManager.OnListItemSelect(m_data);
			}
			else
			{
				// if still processing
				toggle.isOn = false;
			}
		}

		public void PopulateData(SDKJob data, bool isActive) {
			this.data = data;
			
			m_ToggleLock = true;
			toggle.isOn = isActive;
			m_ToggleLock = false;
		}

		private void SetName() {
			if (nameField != null) {
				nameField.text = string.Format("{0}: {1}", m_data.bank, m_data.name);
			}
		}

		private void SetDate() {
			if (dateField != null) {
				DateTime dateCreated = DateTime.Parse(m_data.created).ToLocalTime();
				DateTime dateModified = DateTime.Parse(m_data.modified).ToLocalTime();
				double diffInSeconds = (dateModified - dateCreated).TotalSeconds;
				dateField.text = string.Format("{0}\n({1} secs)", dateCreated.ToString("yyyy-MM-dd HH:mm"), diffInSeconds);
			}
		}

		private void SetIcon() {
			if(iconField != null) {
				switch (mapState) {
					case MapState.Queued:
						iconField.sprite = sprite_queued;
						break;
					case MapState.Processing:
						iconField.sprite = sprite_processing;
						break;
					case MapState.Done:
						iconField.sprite = sprite_done;
						break;
					case MapState.Failed:
						iconField.sprite = sprite_failed;
						break;
					default:
						iconField.sprite = sprite_done;
						break;
				}
			}
		}

		private void Start() {
			if(visualizeManager == null) {
                visualizeManager = GetComponentInParent<VisualizeManager>();
			}
		}
	}
}