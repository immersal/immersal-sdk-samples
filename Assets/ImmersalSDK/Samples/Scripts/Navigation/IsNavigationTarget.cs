/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;

namespace Immersal.Samples.Navigation
{
    public class IsNavigationTarget : MonoBehaviour
    {
        public NavigationTargets.NavigationCategory navigationCategory = NavigationTargets.NavigationCategory.Locations;
        public string targetName;
        public Sprite icon;
        public Vector3 position
        {
            get
            {
                return m_collider.bounds.center;
            }

            set
            {

            }
        }

        private Collider m_collider = null;

        private void Start()
        {
            NavigationGraphManager.Instance?.AddTarget(this);
        }

        private void OnDestroy()
        {
            NavigationGraphManager.Instance?.RemoveTarget(this);
        }

        private void OnEnable()
        {
            m_collider = GetComponent<Collider>();

            if (!NavigationTargets.NavigationTargetsDict.ContainsKey(navigationCategory))
                NavigationTargets.NavigationTargetsDict[navigationCategory] = new List<GameObject>();

            NavigationTargets.NavigationTargetsDict[navigationCategory].Add(gameObject);

            if (targetName.Equals(""))
            {
                targetName = gameObject.name;
            }
        }

        private void OnDisable()
        {
            if (NavigationTargets.NavigationTargetsDict.ContainsKey(navigationCategory))
                NavigationTargets.NavigationTargetsDict[navigationCategory].Remove(gameObject);
        }
    }
}