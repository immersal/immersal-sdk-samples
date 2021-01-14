/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

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
            ARLocalizer.Instance.autoStart = value;
        }

        public void Downsample(bool value)
        {
            ARLocalizer.Instance.downsample = value;
        }

        public void UseFiltering(bool value)
        {
            ARLocalizer.Instance.useFiltering = value;
        }

        public void Pause()
        {
            ARLocalizer.Instance.Pause();
        }

        public void Resume()
        {
            ARLocalizer.Instance.Resume();
        }

        public void StopLocalizing()
        {
            ARLocalizer.Instance.StopLocalizing();
        }
        public void StartLocalizing()
        {
            ARLocalizer.Instance.StartLocalizing();
        }
        public void Localize()
        {
            ARLocalizer.Instance.Localize();
        }

        public void ClosePanel()
        {
            Destroy(this.gameObject);
        }
    }
}
