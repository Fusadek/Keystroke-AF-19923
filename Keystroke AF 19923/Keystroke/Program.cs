
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Gma.System.MouseKeyHook;

namespace Keystroke
{
    class Program
    {
        class KeystrokeData
        {
            public string Key { get; set; }
            public DateTime KeyDownTime { get; set; }
            public DateTime? KeyUpTime { get; set; }

            public double? HoldTime => KeyUpTime.HasValue ? (KeyUpTime.Value - KeyDownTime).TotalMilliseconds : (double?)null;
        }

        static List<KeystrokeData> dataList = new List<KeystrokeData>();
        static Dictionary<Keys, DateTime> pressedKeys = new Dictionary<Keys, DateTime>();
        static IKeyboardMouseEvents hook;

        [STAThread]
        static void Main()
        {
            Console.WriteLine("Pisz coś na klawiaturze. ESC kończy zapis...");

            bool running = true;
            hook = Hook.GlobalEvents();

            hook.KeyDown += (s, e) =>
            {
                var now = DateTime.Now;

                if (!pressedKeys.ContainsKey(e.KeyCode))
                {
                    pressedKeys[e.KeyCode] = now;

                    dataList.Add(new KeystrokeData
                    {
                        Key = e.KeyCode.ToString(),
                        KeyDownTime = now
                    });
                }
            };

            hook.KeyUp += (s, e) =>
            {
                var now = DateTime.Now;

                if (e.KeyCode == Keys.Escape)
                {
                    running = false;
                }

                if (pressedKeys.TryGetValue(e.KeyCode, out var downTime))
                {
                    var entry = dataList.LastOrDefault(d => d.Key == e.KeyCode.ToString() && !d.KeyUpTime.HasValue);
                    if (entry != null)
                    {
                        entry.KeyUpTime = now;
                    }

                    pressedKeys.Remove(e.KeyCode);
                }
            };

            while (running)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(10);
            }

            hook.Dispose();
            SaveData("keystroke_data.csv");
            AnalyzeData();
        }

        private static void SaveData(string path)
        {
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine("Key,KeyDownTime,KeyUpTime,HoldTime");
                foreach (var d in dataList)
                {
                    writer.WriteLine($"{d.Key},{d.KeyDownTime:O},{d.KeyUpTime:O},{d.HoldTime}");
                }
            }

            Console.WriteLine($"Dane zapisane do pliku: {path}");
        }

        private static void AnalyzeData()
        {
            if (dataList.Count < 2)
            {
                Console.WriteLine("Za mało danych do analizy.");
                return;
            }

            var holdAvg = dataList.Where(d => d.HoldTime.HasValue).Average(d => d.HoldTime.Value);

            var kdKdList = new List<double>();
            var kuKdList = new List<double>();

            for (int i = 1; i < dataList.Count; i++)
            {
                var kdKd = (dataList[i].KeyDownTime - dataList[i - 1].KeyDownTime).TotalMilliseconds;
                var kuKd = dataList[i - 1].KeyUpTime.HasValue
                    ? (dataList[i].KeyDownTime - dataList[i - 1].KeyUpTime.Value).TotalMilliseconds
                    : 0;

                kdKdList.Add(kdKd);
                kuKdList.Add(kuKd);
            }

            Console.WriteLine("\n*** Analiza danych ***");
            Console.WriteLine($"Średni Hold Time: {holdAvg:F2} ms");
            Console.WriteLine($"Średni KeyDown–KeyDown Time: {kdKdList.Average():F2} ms");
            Console.WriteLine($"Średni KeyUp–KeyDown Time: {kuKdList.Average():F2} ms");
        }
    }
}
