/*===============================================================================
Copyright (C) 2023 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sales@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Immersal.AR;
using Immersal.REST;
using Immersal.Samples.Mapping;

namespace Immersal.Samples
{
    [RequireComponent(typeof(TMP_Dropdown))]
    public class MethodListController : MonoBehaviour
    {
        private TMP_Dropdown m_Dropdown;
        public MapperSettings m_MapperSettings;

        void Awake()
        {
            m_Dropdown = GetComponent<TMP_Dropdown>();
            m_Dropdown.ClearOptions();
        }

        void Start()
        {
            List<string> methods = new List<string>();

            foreach (SolverType method in Enum.GetValues(typeof(SolverType)))
            {
                methods.Add(method.ToString());
            }

            m_Dropdown.AddOptions(methods);
        }

        public void OnValueChanged(TMP_Dropdown dropdown)
        {
            var values = Enum.GetValues(typeof(SolverType));
            SolverType solverType  = (SolverType)values.GetValue((long)dropdown.value);
            m_MapperSettings.SetSolverType(solverType);
        }
    }
}