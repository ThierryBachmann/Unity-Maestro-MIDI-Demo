using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DemoMPTK
{
    public class HandleLog : MonoBehaviour
    {
        public GameObject Content;
        public Text textPrefab;
        public Scrollbar Scroll;
        public bool Follow { get; set; }

        private Queue<LogMessage> queue = new Queue<LogMessage>();
        private List<Text> messages = new List<Text>();
        private int index = 0;

        private class LogMessage
        {
            public string Message;
            public LogType Type;

            public LogMessage(string msg, LogType type)
            {
                Message = msg;
                Type = type;
            }
        }
        private void Awake()
        {

        }
        // Start is called before the first frame update
        void Start()
        {
            //Scroll.onValueChanged.AddListener((float val) => Debug.Log(val));
            Follow = true;
        }
        private void OnDestroy()
        {
        }

        void OnEnable()
        {
            Application.logMessageReceivedThreaded += QueueLog;
        }

        void OnDisable()
        {
            Application.logMessageReceivedThreaded -= QueueLog;
        }

        void QueueLog(string message, string stackTrace, LogType type)
        {
            queue.Enqueue(new LogMessage(message, type));
        }

        // Update is called once per frame
        void Update()
        {
            //Debug.Log(queue.Count);
            while (queue.Count > 0)
            {
                LogMessage message = queue.Dequeue();
                string[] lines = message.Message.Split(new char[] { '\n' });

                foreach (string l in lines)
                    CreateText(l);
            }

            while (messages.Count > 500)
            {
                Destroy(messages[0].gameObject);
                messages.RemoveAt(0);
            }

            if (Follow)
                Scroll.value = 0;
        }

        public void Clear()
        {
            while (messages.Count > 0)
            {
                Destroy(messages[0].gameObject);
                messages.RemoveAt(0);
            }
        }

        private void CreateText(string log)
        {
            // no Debug Log here! crash Unity

            Text text = Instantiate<Text>(textPrefab);
            text.transform.SetParent(Content.transform);
            text.name = $"Log {index++}";
            text.text = DateTime.Now.ToString() + " " + log;
            text.transform.position = this.transform.position;
            text.transform.localScale = new Vector3(1, 1, 1);
            text.gameObject.SetActive(true);
            messages.Add(text);
            //if (Scroll.value < 0.1f || log.StartsWith("---"))
            //  Scroll.value = 0;
        }
    }
}