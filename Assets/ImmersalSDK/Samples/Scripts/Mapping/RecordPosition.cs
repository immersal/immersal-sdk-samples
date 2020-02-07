using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;

namespace Immersal.Samples.Mapping
{
    [RequireComponent(typeof(Button))]
    public class RecordPosition : MonoBehaviour
    {
        [System.Serializable]
        public class Savefile
        {
            public string content = "";
        }

        [SerializeField]
        private TextMeshProUGUI m_Text = null;
        [SerializeField]
        private TextMeshProUGUI m_Timestamp = null;
        [SerializeField]
        private Mapper m_Mapper = null;

        private bool m_IsRecording = false;
        private float m_Time = 0f;
        private Savefile m_SaveFile = new Savefile();

        void Start()
        {
            if (m_Text != null)
            {
                m_Text.text = "Record";
            }
        }

        void Update()
        {
            if (m_IsRecording)
            {
                RecordData(m_Time);

                m_Time += Time.deltaTime;
            }
        }

        private void RecordData(float timestamp)
        //private void RecordData(Vector3 mapPosition, Vector2 gpsLatLongfloat, Vector2 vgpsLatLong, float timestamp)
        {
            string entry = m_Mapper.GetVGPSData();
            if (entry != null)
            {
                if(m_Timestamp != null)
                {
                    m_Timestamp.text = m_Time.ToString("0.00") + "s";
                }

                entry = string.Format("{0},{1}\n", entry, timestamp);
                m_SaveFile.content += entry;
            }
        }

        private void WriteFile()
        {
            DateTime dt = DateTime.Now;
            string dts = dt.ToString("yyyy-MM-dd-HH-mm-ss");
            string fileName = "vpgsPath_" + dts + ".json";

            string dataPath = Path.Combine(Application.persistentDataPath, fileName);

            Debug.Log("Data path: " + dataPath);
            if (m_SaveFile.content.Length > 0)
            {
                File.WriteAllText(dataPath, m_SaveFile.content.Remove(m_SaveFile.content.Length - 1));
            }
        }

        public void ToggleRecording()
        {
            if (m_Text == null)
            {
                Debug.Log("No text Textmesh Pro Text object specified");
                return;
            }

            if (m_IsRecording)
            {
                // stop
                m_Text.text = "Record";
                m_Time = 0f;

                WriteFile();
            }
            else
            {
                // start
                m_Text.text = "Stop Recording";

                //init savefile
                m_Time = 0f;
                m_SaveFile = new Savefile();
            }

            m_IsRecording = !m_IsRecording;
        }
    }
}
