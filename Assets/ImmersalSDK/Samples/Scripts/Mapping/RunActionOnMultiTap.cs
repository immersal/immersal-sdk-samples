using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class RunActionOnMultiTap : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private UnityEvent m_OnTapped;
    [SerializeField]
    private float m_TimeThreshold = 1f;
    [SerializeField]
    private int m_TapThreshold = 3;

    private float m_Timer = 0f;
    private int m_Taps = 0;

    void Update()
    {
        if(m_Taps > 0)
        {
            m_Timer += Time.deltaTime;

            if(m_Timer > m_TimeThreshold)
            {
                m_Taps = 0;
                m_Timer = 0f;
            }
            else if(m_Taps >= m_TapThreshold)
            {
                m_OnTapped.Invoke();
                m_Taps = 0;
                m_Timer = 0f;
            }
        }
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        m_Taps++;
    }

    public void ToggleGameObject(GameObject go)
    {
        bool currentState = go.activeInHierarchy;
        go.SetActive(!currentState);
    }
}
