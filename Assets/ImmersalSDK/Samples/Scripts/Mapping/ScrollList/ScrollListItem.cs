/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
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
	public class ScrollListItem : MonoBehaviour
	{
		public enum MapState { Queued, Processing, Sparse, Done, Failed };
		public MapState mapState = MapState.Done;

		private SDKJob m_Data = null;

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

		public SDKJob data
		{
			get { return m_Data; }
			set
			{
				m_Data = value;
				switch (m_Data.status)
				{
					case "done":
					mapState = MapState.Done; break;
					case "sparse":
					mapState = MapState.Sparse; break;
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

		public void OnClick()
		{
			toggle.isOn = !toggle.isOn;
		}

		public void OnToggleChanged(bool value)
		{
			SelectListItem();
		}

		private void SelectListItem()
		{
			if (visualizeManager != null && (mapState == MapState.Sparse || mapState == MapState.Done))
			{
                visualizeManager.OnListItemSelect(data);
			}
			else
			{
				// if still processing
				toggle.SetIsOnWithoutNotify(false);
			}
		}

		public void DeleteItem()
		{
			if (visualizeManager != null)
			{
				visualizeManager.activeFunctionJob = data;
				visualizeManager.ToggleDeletePrompt(true);
			}
		}

		public void RestoreItem()
		{
			if (visualizeManager != null)
			{
				visualizeManager.activeFunctionJob = data;
				visualizeManager.ToggleRestoreMapImagesPrompt(true);
			}
		}

		public void PopulateData(SDKJob data, bool isActive)
		{
			this.data = data;
			
			toggle.SetIsOnWithoutNotify(isActive);
		}

		private void SetName()
		{
			if (nameField != null)
			{
				nameField.text = string.Format("{0}: {1}", m_Data.bank, m_Data.name);
			}
		}

		private void SetDate()
		{
			if (dateField != null)
			{
				DateTime dateCreated = DateTime.Parse(m_Data.created).ToLocalTime();
				DateTime dateModified = DateTime.Parse(m_Data.modified).ToLocalTime();
				double diffInSeconds = (dateModified - dateCreated).TotalSeconds;
				dateField.text = string.Format("{0}\n({1} secs)", dateCreated.ToString("yyyy-MM-dd HH:mm"), diffInSeconds);
			}
		}

		private void SetIcon()
		{
			if (iconField != null)
			{
				switch (mapState)
				{
					case MapState.Queued:
						iconField.sprite = sprite_queued;
						break;
					case MapState.Processing:
						iconField.sprite = sprite_processing;
						break;
					case MapState.Failed:
						iconField.sprite = sprite_failed;
						break;
					default:	// Sparse & Done
						iconField.sprite = sprite_done;
						break;
				}
			}
		}

		private void Start()
		{
			if (visualizeManager == null)
			{
                visualizeManager = GetComponentInParent<VisualizeManager>();
			}
		}
	}
}