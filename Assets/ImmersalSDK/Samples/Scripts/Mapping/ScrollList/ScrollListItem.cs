/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

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
		
		private SDKJob m_Data = default;

		public SDKJob data
		{
			get { return m_Data; }
			set
			{
				m_Data = value;

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
			if (visualizeManager != null && (m_Data.status == SDKJobState.Sparse || m_Data.status == SDKJobState.Done))
			{
				if (VisualizeManager.loadJobs.Contains(data.id))
				{
					toggle.SetIsOnWithoutNotify(true);
				}
				else
				{
					if (toggle.isOn)
					{
						VisualizeManager.loadJobs.Add(data.id);
					}
					
	                visualizeManager.OnListItemSelect(data);
				}
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
				nameField.text = m_Data.name;
			}
		}

		private void SetDate()
		{
			if (dateField != null)
			{
				DateTime dateCreated = DateTime.Parse(m_Data.created).ToLocalTime();
				DateTime dateModified = DateTime.Parse(m_Data.modified).ToLocalTime();
				dateField.text = dateCreated.ToString("yyyy-MM-dd HH:mm");
			}
		}

		private void SetIcon()
		{
			if (iconField != null)
			{
				switch (m_Data.status)
				{
					case SDKJobState.Pending:
						iconField.sprite = sprite_queued;
						break;
					case SDKJobState.Processing:
						iconField.sprite = sprite_processing;
						break;
					case SDKJobState.Failed:
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