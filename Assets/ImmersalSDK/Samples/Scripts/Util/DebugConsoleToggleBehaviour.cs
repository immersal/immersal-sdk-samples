using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugConsoleToggleBehaviour : MonoBehaviour
{
    private Toggle m_Toggle;
    void Awake()
    {
        m_Toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        m_Toggle.isOn = DebugConsole.Instance.isShown;
    }
}
