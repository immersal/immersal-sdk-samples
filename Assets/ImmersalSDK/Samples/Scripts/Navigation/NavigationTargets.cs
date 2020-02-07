/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of Immersal SDK v1.3.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System.Collections.Generic;
using UnityEngine;

namespace Immersal.Samples.Navigation
{
    public class NavigationTargets
    {
        public enum NavigationCategory { People, Locations };
        public NavigationCategory navigationCategories = NavigationCategory.Locations;
        public static Dictionary<NavigationCategory, List<GameObject>> NavigationTargetsDict = new Dictionary<NavigationCategory, List<GameObject>>();
    }
}
