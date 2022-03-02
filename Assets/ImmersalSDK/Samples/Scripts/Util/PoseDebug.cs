using System.Collections;
using System.Collections.Generic;
using Immersal.AR;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Assertions;

public class PoseDebug : MonoBehaviour
{
    [SerializeField]
    private Toggle _orientationToggle = null;
    [SerializeField]
    private Toggle _handednessToggle = null;
    
    private TextMeshProUGUI _textMeshProUGUI = null;
    private Transform _cameraTransform = null;
    private bool isInitialized = false;

    void Start()
    {
        _textMeshProUGUI = GetComponent<TextMeshProUGUI>();
        _cameraTransform = Camera.main.transform;
        
        Assert.IsNotNull(_textMeshProUGUI);
        Assert.IsNotNull(_cameraTransform);
        Assert.IsNotNull(_orientationToggle);
        Assert.IsNotNull(_handednessToggle);

        isInitialized = true;
    }
    
    void Update()
    {
        if (isInitialized)
        {
            Quaternion rot = _cameraTransform.rotation;
            Vector3 pos = _cameraTransform.position;
            if (_orientationToggle.isOn)
            {
                ARHelper.GetRotation(ref rot);
            }

            if (_handednessToggle.isOn)
            {
                Matrix4x4 r = ARHelper.SwitchHandedness(Matrix4x4.Rotate(rot));
                rot = r.rotation;
                pos = ARHelper.SwitchHandedness(pos);
                
                // Just to debug that the order of the matrix sent to Immersal Cloud Service is the same as in Unity (column-major)
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Debug.Log(string.Format(
                        "r00:{0}\tr01: {1}\tr02: {2}\nr10: {3}\tr11: {4}\tr12: {5}\nr20: {6}\tr21: {7}\tr22: {8}\n",
                        r.m00.ToString("F3"), r.m01.ToString("F3"), r.m02.ToString("F3"),
                        r.m10.ToString("F3"), r.m11.ToString("F3"), r.m12.ToString("F3"),
                        r.m20.ToString("F3"), r.m21.ToString("F3"), r.m22.ToString("F3")
                    ));
                }
            }
            
            Matrix4x4 m = Matrix4x4.TRS(pos, rot, Vector3.one);
            string text = m.ToString("+0.00;-0.00;+0.00");
            _textMeshProUGUI.text = text;
        }    
    }
}

//
// Camera cam = this.mainCamera;
// ARHelper.GetIntrinsics(out j.intrinsics);
// Quaternion rot = cam.transform.rotation;
// Vector3 pos = cam.transform.position;
// ARHelper.GetRotation(ref rot);
// j.rotation = ARHelper.SwitchHandedness(Matrix4x4.Rotate(rot));
// j.position = ARHelper.SwitchHandedness(pos);