/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Immersal.REST;

namespace Immersal.Samples.Mapping.ScrollList
{
	[RequireComponent(typeof(ScrollRect))]
	public class ScrollListControl : MonoBehaviour {

		[SerializeField]
		private GameObject itemTemplate = null;

		[SerializeField]
		private Transform contentParent = null;
		private ScrollRect scrollRect;
		private List<GameObject> items = new List<GameObject>();
		private SDKJob[] m_Data;
		private List<int> m_ActiveMaps;
		
		[SerializeField]
		private GameObject loadMoreButton = null;
		[SerializeField]
		private int displayedItemCount = 50;
		
		public void SetData(SDKJob[] data, List<int> activeMaps)
		{
			bool newDataAvailable = false;
			if (m_Data != null)
			{
				newDataAvailable = data.Length > m_Data.Length;
			}
			m_Data = data;
			m_ActiveMaps = activeMaps;
			GenerateItems(0, items.Count==0?displayedItemCount:items.Count, false, newDataAvailable);
		}

		public void LoadMore()
		{
			GenerateItems(items.Count, items.Count + displayedItemCount, true);
		}

		public void GenerateItems(int from, int to, bool append = false, bool newDataAvailable = false)
		{
			to = Mathf.Clamp(to, 1, m_Data.Length);
			
			if (to == items.Count && !newDataAvailable)
			{
				//update exising items
				for (int i = 0; i < items.Count; i++)
				{
					ScrollListItem scrollListItem = items[i].GetComponent<ScrollListItem>();
					scrollListItem.data = m_Data[i];
				}
				return;
			}
			
			if (items.Count > 0 && !append) {
				DestroyItems();
			}

			if (m_Data != null && m_Data.Length > 0)
			{
				for (int i = from; i < to; i++) {
					SDKJob job = m_Data[i];
					GameObject item = Instantiate(itemTemplate);
					items.Add(item);
					item.name = item.name + "_" + i;
					item.SetActive(true);
					item.transform.SetParent(contentParent, false);
			
					ScrollListItem scrollListItem = item.GetComponent<ScrollListItem>();
					scrollListItem.PopulateData(job, IsActive(job.id));
				}
			}
			
			if (newDataAvailable) 
			{
				ScrollToTop();
			}

			loadMoreButton.SetActive(m_Data.Length > to);
			loadMoreButton.transform.SetAsLastSibling();
		}

		private bool IsActive(int mapId)
		{
			foreach (int id in m_ActiveMaps)
			{
				if (mapId == id)
					return true;
			}
			return false;
		}

		public void DestroyItems()
		{
			foreach (GameObject item in items) {
				Destroy(item);
			}
			items.Clear();
		}

		private void ScrollToTop() {
			if (scrollRect != null)
				scrollRect.normalizedPosition = new Vector2(0, 1);
		}

		private void ScrollToBottom() {
			if (scrollRect != null)
				scrollRect.normalizedPosition = new Vector2(0, 0);
		}

		private void OnEnable() {
			scrollRect = GetComponent<ScrollRect>();
			ScrollToTop();
		}
	}
}