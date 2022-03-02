/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;
using Immersal.REST;
using Immersal.AR;

namespace Immersal.Samples.Mapping.ActiveMapsList
{
    [RequireComponent(typeof(ScrollRect))]
    public class ActiveMapsListControl : MonoBehaviour
    {
        [SerializeField]
        private VisualizeManager m_VisualizeManager = null;
        [SerializeField]
        private MapperSettings mapperSettings = null;
		[SerializeField]
		private GameObject itemTemplate = null;
		[SerializeField]
		private Transform contentParent = null;
		private ScrollRect scrollRect;
        private List<GameObject> items = new List<GameObject>();
        private List<ARMap> m_ActiveMaps = new List<ARMap>();
        private ToggleGroup m_toggleGroup = null;
        public int rootMapId = -1;

		public void GenerateItems()
        {
            DestroyItems();

            m_ActiveMaps.Clear();
            items.Clear();

            foreach(KeyValuePair<int, ARMap> entry in ARSpace.mapIdToMap)
            {
                m_ActiveMaps.Add(entry.Value);
                
                GameObject item = Instantiate(itemTemplate);
                items.Add(item);
                item.SetActive(true);
                item.transform.SetParent(contentParent, false);

                ActiveMapsListItem activeMapsListItem = item.GetComponent<ActiveMapsListItem>();
                activeMapsListItem.SetMapId(entry.Value.mapId);
                activeMapsListItem.SetName(entry.Value.mapName);
                activeMapsListItem.SetToggleGroup(m_toggleGroup);
                activeMapsListItem.SetListController(this);
            }
            
            if (items.Count > 0)
            {
                items[0].GetComponent<ActiveMapsListItem>().SetStateManually(true);
            }
        }

		public void DestroyItems()
        {
			foreach (GameObject item in items) {
				Destroy(item);
			}

			items.Clear();
		}

		private void ScrollToTop()
        {
			if (scrollRect != null)
				scrollRect.normalizedPosition = new Vector2(0, 1);
		}

		private void ScrollToBottom()
        {
			if (scrollRect != null)
				scrollRect.normalizedPosition = new Vector2(0, 0);
		}

		private void OnEnable()
        {
            if(m_toggleGroup == null)
            {
                m_toggleGroup = GetComponent<ToggleGroup>();
            }

			GenerateItems();
			scrollRect = GetComponent<ScrollRect>();
			ScrollToTop();
		}

        public async void SubmitAlignment()
        {
            if(mapperSettings == null)
            {
                Debug.Log("ActiveMapListControl: MapperSettings not found");
                return;
            }

            bool transformRootToOrigin = mapperSettings.transformRootToOrigin;

            // Debug.Log(string.Format("clicked, root id: {0}, map count {1}", rootMapId, m_ActiveMaps.Count));
            if(rootMapId > 0 && m_ActiveMaps.Count > 1)
            {
                Transform root = ARSpace.mapIdToMap[rootMapId].transform;
                Matrix4x4 worldSpaceRoot = root.localToWorldMatrix;

                foreach(KeyValuePair<int, ARMap> entry in ARSpace.mapIdToMap)
                {
                    if(entry.Value.mapId != rootMapId)
                    {
                        // Debug.Log(string.Format("looping... {0}", entry.Value.mapId));
                        Transform xf = entry.Value.transform;
                        Matrix4x4 worldSpaceTransform = xf.localToWorldMatrix;

                        if(transformRootToOrigin)
                        {
                            Matrix4x4 offset = worldSpaceRoot.inverse * worldSpaceTransform;
                            await MapAlignmentSave(entry.Value.mapId, offset);
                        }
                        else
                        {
                            // TODO: implement ECEF/double support
                            Vector3 rootPosMetadata = new Vector3((float)ARSpace.mapIdToMap[rootMapId].mapAlignment.tx, (float)ARSpace.mapIdToMap[rootMapId].mapAlignment.ty, (float)ARSpace.mapIdToMap[rootMapId].mapAlignment.tz);
                            Quaternion rootRotMetadata = new Quaternion((float)ARSpace.mapIdToMap[rootMapId].mapAlignment.qx, (float)ARSpace.mapIdToMap[rootMapId].mapAlignment.qy, (float)ARSpace.mapIdToMap[rootMapId].mapAlignment.qz, (float)ARSpace.mapIdToMap[rootMapId].mapAlignment.qw);

                            // IMPORTANT
                            // Switch coordinate system handedness back from Immersal Cloud Service's default right-handed system to Unity's left-handed system
                            Matrix4x4 b = Matrix4x4.TRS(rootPosMetadata, rootRotMetadata, Vector3.one);
                            Matrix4x4 a = ARHelper.SwitchHandedness(b);

                            Matrix4x4 offset = a * worldSpaceRoot.inverse * worldSpaceTransform;
                            await MapAlignmentSave(entry.Value.mapId, offset);
                        }
                    }
                    else
                    {
                        if(transformRootToOrigin)
                        {
                            // set root to origin
                            Matrix4x4 identity = Matrix4x4.identity;
                            await MapAlignmentSave(entry.Value.mapId, identity);
                        }
                        else
                        {
                            // or keep the root transform
                            // await MapAlignmentSave(entry.Value.mapId, worldSpaceRoot);
                        }
                    }
                }

                m_VisualizeManager?.DefaultView();
                Immersal.Samples.Mapping.NotificationManager.Instance.GenerateSuccess("Map Alignments Saved");
            }
        }

        private async Task MapAlignmentSave(int mapId, Matrix4x4 m)
        {
            //
            // Updates map metadata to the Cloud Service and reloads to keep local files in sync
            //
            Vector3 pos = m.GetColumn(3);
            Quaternion rot = m.rotation;
            float scl = (m.lossyScale.x + m.lossyScale.y + m.lossyScale.z) / 3f; // Only uniform scale metadata is supported

            // IMPORTANT
            // Switching coordinate system handedness from Unity's left-handed system to Immersal Cloud Service's default right-handed system
            Matrix4x4 b = Matrix4x4.TRS(pos, rot, transform.localScale);
            Matrix4x4 a = ARHelper.SwitchHandedness(b);
            pos = a.GetColumn(3);
            rot = a.rotation;

            // Update map alignment metadata to Immersal Cloud Service
            JobMapAlignmentSetAsync j = new JobMapAlignmentSetAsync();
            j.id = mapId;
            j.tx = pos.x;
            j.ty = pos.y;
            j.tz = pos.z;
            j.qx = rot.x;
            j.qy = rot.y;
            j.qz = rot.z;
            j.qw = rot.w;
            j.scale = scl;

            j.OnResult += (SDKMapAlignmentSetResult result) =>
            {
                Debug.Log(string.Format("Alignment for map {0} saved", mapId));
            };

            j.OnError += (e) =>
            {
                Immersal.Samples.Mapping.NotificationManager.Instance.GenerateError("Network Error");
                Debug.Log(string.Format("Failed to save alignment for map id {0}\n{1}", mapId, e));
            };

            await j.RunJobAsync();
        }
	}
}
