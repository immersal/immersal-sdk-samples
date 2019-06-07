/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal AR Cloud SDK Early Access project.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
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
		private MappingUIManager manager = null;
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

		public void OnClick() {
			toggle.isOn = !toggle.isOn;
		}

		public void OnToggleChanged(bool value)
		{
			SelectListItem();
		}

		private void SelectListItem()
		{
			if (manager != null && m_data.status == "done")
			{
				manager.OnListItemSelect(m_data);
				SetActiveMap();
			}
			else
			{
				// if still processing
				toggle.isOn = false;
			}
		}

		public void SetActiveMap() {
			if(manager != null) {
				manager.UpdateActiveMapName(string.Format("{0}: {1}", m_data.bank, m_data.name), false);
			}
		}

		public void PopulateData(SDKJob data) {
			m_data = data;
			
			switch (data.status)
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
			if(manager == null) {
				manager = transform.root.GetComponent<MappingUIManager>();
			}
		}
	}
}