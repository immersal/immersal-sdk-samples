/*===============================================================================
Copyright (C) 2019 Immersal Ltd. All Rights Reserved.

This file is part of Immersal AR Cloud SDK v1.1.

The Immersal AR Cloud SDK cannot be copied, distributed, or made available to
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

        private void Awake()
        {
            if (!NavigationTargets.NavigationTargetsDict.ContainsKey(navigationCategory))
                NavigationTargets.NavigationTargetsDict[navigationCategory] = new List<GameObject>();

            NavigationTargets.NavigationTargetsDict[navigationCategory].Add(gameObject);

            if (targetName.Equals(""))
            {
                targetName = gameObject.name;
            }
        }

        private void OnDestroy()
        {
            if (NavigationTargets.NavigationTargetsDict.ContainsKey(navigationCategory))
                NavigationTargets.NavigationTargetsDict[navigationCategory].Remove(gameObject);
        }
    }
}