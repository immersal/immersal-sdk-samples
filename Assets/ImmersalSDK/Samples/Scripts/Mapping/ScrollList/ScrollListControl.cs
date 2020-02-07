/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

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

		public void SetData(SDKJob[] data, List<int> activeMaps)
		{
			m_Data = data;
			m_ActiveMaps = activeMaps;
			GenerateItems();
		}

		public void GenerateItems() {
			int newDataLen = m_Data == null ? 0 : m_Data.Length;
			bool scroll = items.Count != newDataLen;

			if (!scroll)
			{
				for (int i = 0; i < items.Count; i++)
				{
					ScrollListItem scrollListItem = items[i].GetComponent<ScrollListItem>();
					scrollListItem.data = m_Data[i];
				}
				return;
			}

			if (items.Count > 0) {
				DestroyItems();
			}

			if (m_Data != null && m_Data.Length > 0)
			{
				for (int i = 0; i < m_Data.Length; i++) {
					SDKJob job = m_Data[i];
					GameObject item = Instantiate(itemTemplate);
					items.Add(item);
					item.SetActive(true);
					item.transform.SetParent(contentParent, false);

					ScrollListItem scrollListItem = item.GetComponent<ScrollListItem>();
					scrollListItem.PopulateData(job, IsActive(job.id));
				}
			}

			if (scroll)
			{
				ScrollToTop();
			}
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

		public void DestroyItems() {
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
			GenerateItems();
			scrollRect = GetComponent<ScrollRect>();
			ScrollToTop();
		}
	}
}