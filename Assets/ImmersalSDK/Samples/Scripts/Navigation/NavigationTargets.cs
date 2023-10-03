/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
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
