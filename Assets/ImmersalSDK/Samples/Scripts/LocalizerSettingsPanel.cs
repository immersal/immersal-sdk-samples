/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using Immersal.AR;

namespace Immersal.Samples
{
    public class LocalizerSettingsPanel : MonoBehaviour
    {
        public void AutoStart(bool value)
        {
            ImmersalSDK.Instance.Localizer.autoStart = value;
        }

        public void Downsample(bool value)
        {
            ImmersalSDK.Instance.downsample = value;
        }

        public void UseFiltering(bool value)
        {
            ImmersalSDK.Instance.Localizer.useFiltering = value;
        }

        public void Pause()
        {
            ImmersalSDK.Instance.Localizer.Pause();
        }

        public void Resume()
        {
            ImmersalSDK.Instance.Localizer.Resume();
        }

        public void StopLocalizing()
        {
            ImmersalSDK.Instance.Localizer.StopLocalizing();
        }
        public void StartLocalizing()
        {
            ImmersalSDK.Instance.Localizer.StartLocalizing();
        }
        public void Localize()
        {
            ImmersalSDK.Instance.Localizer.Localize();
        }

        public void ClosePanel()
        {
            Destroy(this.gameObject);
        }
    }
}
