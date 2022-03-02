/*===============================================================================
Copyright (C) 2022 Immersal - Part of Hexagon. All Rights Reserved.

This file is part of the Immersal SDK.

The Immersal SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of Immersal Ltd.

Contact sdk@immersal.com for licensing requests.
===============================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DebugConsole : MonoBehaviour
{
    [SerializeField] bool  m_ShowOnAppStart;
    [SerializeField] private TextMeshProUGUI m_DebugLogText;
    [SerializeField] private GameObject m_ContentParent;
    [SerializeField] private int m_DebugLogLineMaxCount = 100;
    
    private List<string> m_DebugLogLines = new List<string>();
    private StringBuilder m_stringBuilder = new StringBuilder();
    
    private static DebugConsole m_instance = null;
    
    public static DebugConsole Instance
    {
        get
        {
#if UNITY_EDITOR
            if (m_instance == null && !Application.isPlaying)
            {
                m_instance = FindObjectOfType<DebugConsole>();
            }
#endif
            if (m_instance == null)
            {
                Debug.LogError("No DebugConsole instance found. Ensure one exists in the scene.");
            }
            return m_instance;
        }
    }

    void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
        if (m_instance != this)
        {
            Debug.LogError("There must be only one DebugConsole object in a scene.");
            DestroyImmediate(this);
        }
    }
    void Start()
    {
        m_ContentParent.SetActive(m_ShowOnAppStart);
    }
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    public bool isShown => m_ContentParent.activeSelf;
    public void Show(bool show)
    {
        if (m_ContentParent.activeSelf == show) return;
        
        m_ContentParent.SetActive(show);
        Debug.Log("Debug console "+(show?"enabled":"disabled"));
    }
    
    public void ToggleShow()
    {
        Show(!m_ContentParent.activeSelf);
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Warning:
                logString = "<color=orange>" + logString + "</color>";
                break;
            case LogType.Error:
            case LogType.Exception:
                logString = "<color=red>" + logString + Environment.NewLine + "STACK TRACE: " + stackTrace + "</color>";
                break;
        }
        AddText(logString);
    }

    private void AddText(string text)
    {
        if (m_DebugLogLines.Count >= m_DebugLogLineMaxCount)
        {
            m_DebugLogLines.RemoveAt(0);
        }

        m_DebugLogLines.Add("[" + Time.realtimeSinceStartup.ToString("0.000") + "] " + text);

        m_DebugLogText.text = ConstructLogText();
    }
    
    private string ConstructLogText()
    {
        m_stringBuilder.Clear();
        
        for (int i = m_DebugLogLines.Count - 1; i >= 0; i--)
        {
            m_stringBuilder.AppendLine(m_DebugLogLines[i]);
        }

        return m_stringBuilder.ToString();
    }
}
