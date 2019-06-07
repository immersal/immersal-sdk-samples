using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateOverTime : MonoBehaviour
{
    [SerializeField]
    private Vector3 m_Axis = new Vector3(0f, 1, 0f);
    [SerializeField]
    private float m_Speed = 40f;

    void Update()
    {
        transform.Rotate(m_Axis, m_Speed * Time.deltaTime);
    }
}
