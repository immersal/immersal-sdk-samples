using System;
using UnityEngine;
using Immersal;
using Immersal.AR;
using Immersal.REST;

public class NRAppController : MonoBehaviour, IJobHost
{
    [SerializeField]
    private ARMap m_ARMap = null;
    [SerializeField]
    private int mapServerId = 0;   // change this to your map server ID

    private ImmersalSDK m_Sdk;

    public string server
    {
        get { return m_Sdk.localizationServer; }
    }

    public string token
    {
        get { return m_Sdk.developerToken; }
    }

    // Start is called before the first frame update
    void Start()
    {
        m_Sdk = ImmersalSDK.Instance;

        if (mapServerId > 0)
        {
            LoadMap(mapServerId);
        }
    }

    public void LoadMap(int id)
    {
        CoroutineJobLoadMap j = new CoroutineJobLoadMap();
        j.host = this;
        j.id = id;
        j.OnSuccess += (SDKMapResult result) =>
        {
            if (result.error == "none")
            {
                byte[] mapData = Convert.FromBase64String(result.b64);
                Debug.Log(string.Format("Load map {0} ({1} bytes)", id, mapData.Length));
                this.m_ARMap.LoadMap(mapData);
            }
        };

        StartCoroutine(j.RunJob());
    }
}