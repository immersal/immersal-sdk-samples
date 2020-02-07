using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Transform m_lookAt = null;

    void Start()
    {
        m_lookAt = Camera.main.transform;    
    }

    // Update is called once per frame
    void Update()
    {
        if(m_lookAt)
        {
            Vector3 direction = m_lookAt.position - transform.position;
            Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);

            Quaternion rotation = Quaternion.LookRotation(-flatDirection, Vector3.up);

            transform.rotation = rotation;
        }
    }
}
