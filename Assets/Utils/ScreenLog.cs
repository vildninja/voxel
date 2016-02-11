using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UI;

namespace VildNinja.Utils
{
    public class ScreenLog : MonoBehaviour
    {
        private struct Line
        {
            public string text;
            public float time;
        }

        private static readonly Dictionary<string, string> sticky = new Dictionary<string, string>();
        private static readonly List<Line> log = new List<Line>();

        private static float lastAdded = 0;

        public static void SetSticky(string name, string value)
        {
            if (sticky.ContainsKey(name))
            {
                sticky[name] = value;
            }
            else
            {
                sticky.Add(name, value);
            }
            lastAdded = Time.unscaledTime;
        }

        public static void RemoveSticky(string name)
        {
            sticky.Remove(name);
            lastAdded = Time.unscaledTime;
        }

        public static void Write(string line)
        {
            log.Add(new Line {text = line, time = Time.unscaledTime + 5});
            lastAdded = Time.unscaledTime;

            while (log[0].time < Time.unscaledTime)
            {
                log.RemoveAt(0);
            }
        }

        public int lines = 15;
        private float lastUpdateTime;

        public Text display;

        // Update is called once per frame
        private void Update()
        {
            if (lastUpdateTime < lastAdded || lastUpdateTime + 1 < Time.unscaledTime)
            {
                lastUpdateTime = Time.unscaledTime;

                string text = "";
                int lineCount = 0;

                foreach (var set in sticky)
                {
                    text += set.Key + " => " + set.Value + "\n";
                    lineCount++;
                }

                for (int i = 1; i <= lines && i <= log.Count; i++)
                {
                    if (log[log.Count - i].time < Time.unscaledTime)
                    {
                        break;
                    }

                    text += log[log.Count - i].text + "\n";

                    lineCount++;
                    if (lineCount >= lines)
                    {
                        break;
                    }
                }

                display.text = text;
            }
        }
    }
}
