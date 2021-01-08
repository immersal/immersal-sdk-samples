/*===============================================================================
Copyright (C) 2020 Immersal Ltd. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Immersal.Samples.Util
{
    public class HorizontalProgressBar : MonoBehaviour
    {
        [SerializeField]
        private Image m_ForegroundImage = null;
        [SerializeField]
        private TextMeshProUGUI m_LabelText = null;
        private int m_CurrentValue = 0;

        public int minValue = 0;
        public int maxValue = 100;

        public int currentValue
        {
            get => m_CurrentValue;
            set
            {
                m_CurrentValue = value;
                m_ForegroundImage.fillAmount = (float)value / (float)maxValue;
                //m_LabelText.text = string.Format("{0}/{1}", value, maxValue);
                m_LabelText.text = string.Format("{0}%", value);
            }
        }

        void Start()
        {
            Reset();
        }

        public void Reset()
        {
            currentValue = 0;
        }
    }
}