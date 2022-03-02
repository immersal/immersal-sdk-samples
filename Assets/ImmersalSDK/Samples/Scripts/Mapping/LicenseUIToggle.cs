using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LicenseUIToggle : MonoBehaviour
{
    [SerializeField] private GameObject m_licensePage;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleVisibility()
    {
        m_licensePage.SetActive(!m_licensePage.activeSelf);
    }
}
