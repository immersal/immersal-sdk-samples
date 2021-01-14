using System.Collections;
using UnityEngine;
using TMPro;
using Immersal;
using Immersal.AR;
#if HWAR
using HuaweiARUnitySDK;
#endif

public class AppController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI m_ARCoreDebugText = null;
    [SerializeField]
    private TextMeshProUGUI m_HWARDebugText = null;
    [SerializeField]
    private UnityEngine.XR.ARFoundation.ARSession m_ARSession = null;
    [SerializeField]
    private GameObject m_ARSessionOrigin = null;
    [SerializeField]
    private Common.SessionComponent m_HWARSession = null;

    private LocalizerBase m_Localizer = null;
    private bool m_SDKStarted = false;

    // Start is called before the first frame update
    void Start()
    {
        m_ARCoreDebugText.text = "";
        m_HWARDebugText.text = "";

        // auto-start
        //StartAREngine(AREngineType.HuaweiAR);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_SDKStarted) return;

        bool isHWARTracking = false;
        bool isARCoreTracking = false;

        #if HWAR
        isHWARTracking = ARFrame.GetTrackingState() == ARTrackable.TrackingState.TRACKING;
        #endif
        isARCoreTracking = UnityEngine.XR.ARFoundation.ARSession.state == UnityEngine.XR.ARFoundation.ARSessionState.SessionTracking;

        if (isHWARTracking || isARCoreTracking)
        {
            OnStartImmersal();
        }
    }

    public enum AREngineType
    {
        HuaweiAR = 0,
        ARCore = 1
    }

    public void StartAREngine(AREngineType type)
    {
        switch (type)
        {
            case AREngineType.HuaweiAR:
                m_HWARSession.gameObject.SetActive(true);
                break;
            case AREngineType.ARCore:
                StartCoroutine(CheckSupportARCore());
                break;
        }
    }

    public void LogARCore(string message)
    {
        m_ARCoreDebugText.text += $"{message}\n";
    }

    public void LogHWAR(string message)
    {
        m_HWARDebugText.text += $"{message}\n";
    }

    public void OnStartARCore()
    {
        LogARCore("Starting ARCore...");
        m_ARSession.gameObject.SetActive(true);
        StartCoroutine(CheckSupportARCore());
    }

    public void OnStopARCore()
    {
        LogARCore("Stopping ARCore...");
        Destroy(m_ARSessionOrigin);
        Destroy(m_ARSession.gameObject);
    }

    public void OnStartHwAr()
    {
        #if HWAR
        LogHWAR("Starting HWAR...");
        m_HWARSession.gameObject.SetActive(true); // calls Start() -> Init() in SessionComponent.cs
        #endif
    }

    public void OnStopHwAR()
    {
        #if HWAR
        LogHWAR("Stopping HWAR...");
        HuaweiARUnitySDK.ARSession.Stop();
        m_HWARSession.gameObject.SetActive(false);
        #endif
    }

    public void OnStartImmersal()
    {
        if (m_SDKStarted) return;

        GameObject go = ImmersalSDK.Instance.gameObject;

        Debug.Log("Starting Immersal SDK...");

        if (ImmersalSDK.isHWAR)
        {
            #if HWAR
            m_Localizer = go.AddComponent<HWARLocalizer>();
            Debug.Log("Adding HWARLocalizer");
            #endif
        }
        else
        {
            m_Localizer = go.AddComponent<ARLocalizer>();
            Debug.Log("Adding ARLocalizer");
        }

        m_Localizer.localizationInterval = 1f;
        m_Localizer.downsample = true;
        m_Localizer.enabled = true;

        m_SDKStarted = true;
    }

    IEnumerator CheckSupportARCore()
    {
        //SetInstallButtonActive(false);

        LogARCore("Checking for AR support...");

        yield return UnityEngine.XR.ARFoundation.ARSession.CheckAvailability();

        if (UnityEngine.XR.ARFoundation.ARSession.state == UnityEngine.XR.ARFoundation.ARSessionState.NeedsInstall)
        {
            LogARCore("Your device supports AR, but requires a software update.");
            LogARCore("Attempting install...");
            yield return UnityEngine.XR.ARFoundation.ARSession.Install();
        }

        if (UnityEngine.XR.ARFoundation.ARSession.state == UnityEngine.XR.ARFoundation.ARSessionState.Ready)
        {
            LogARCore("Your device supports AR!");
            LogARCore("Starting AR session...");

            // To start the ARSession, we just need to enable it.
            m_ARSessionOrigin.SetActive(true);
            m_ARSession.enabled = true;

            ImmersalSDK.isHWAR = false;
        }
        else
        {
            switch (UnityEngine.XR.ARFoundation.ARSession.state)
            {
                case UnityEngine.XR.ARFoundation.ARSessionState.Unsupported:
                    LogARCore("Your device does not support AR.");
                    break;
                case UnityEngine.XR.ARFoundation.ARSessionState.NeedsInstall:
                    LogARCore("The software update failed, or you declined the update.");

                    // In this case, we enable a button which allows the user
                    // to try again in the event they decline the update the first time.
                    //SetInstallButtonActive(true);
                    break;
            }

            LogARCore("\n[Start non-AR experience instead]");

            //
            // Start a non-AR fallback experience here...
            //
        }
    }
}
