using System;
using System.Diagnostics;
using System.IO;
using Terminal.Gui;

namespace OpenTap.Tui.Views
{
    public class Recovery
    {
        public class RecFile {
            public string FilePath { get; set; }
            public string TestPlan { get; set; }
        }
        public RecFile file = new RecFile();
        public string FilePath { get => file.FilePath; set => file.FilePath = value; }
        public string TestPlan { get => file.TestPlan; set => file.TestPlan = value; }

        private TestPlan plan = new TestPlan();
        public TestPlan Plan
        {
            get
            {
                return plan;
            }
            set 
            {
                plan = value;
                FilePath = plan.Path;
                Save();
                TestPlanChanged?.Invoke(plan);
            }
        }

        private Stream recStream;

        public event Action<TestPlan> TestPlanChanged;

        public Recovery()
        {
            recStream = File.OpenWrite($".{Process.GetCurrentProcess().Id}.TuiRecovery");
            MainWindow.UnsavedChangesCreated += Save;
            Application.MainLoop.Invoke(() =>
            {
                if (string.IsNullOrEmpty(plan.Path))
                {
                    if (!Load())
                    {
                        Plan = new TestPlan();
                    }
                }
            });
        }

        private static TapSerializer TapSerializer = new TapSerializer();

        public void Save()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Plan.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms))
                {
                    TestPlan = sr.ReadToEnd();
                }
            }

            recStream.Seek(0, SeekOrigin.Begin);
            TapSerializer.Serialize(recStream, file);
        }

        public bool Load()
        {
            string[] files = Directory.GetFiles("./", ".*.TuiRecovery");
            if (files.Length == 0)
                return false;

            foreach (var file in files)
            {
                RecFile recfile = null;
                try
                {
                    recfile = TapSerializer.DeserializeFromFile(file, type: TypeData.FromType(typeof(Recovery))) as RecFile;
                    if (recfile == null)
                        continue;
                }
                catch
                {
                    continue;
                }
                TUI.Log.Debug("Recovery test plan detected." + file);
                File.Delete(file);
                MainWindow.ContainsUnsavedChanges = true;

                using (MemoryStream ms = new MemoryStream(recfile.TestPlan.Length * 2))
                {
                    StreamWriter sw = new StreamWriter(ms);
                    sw.Write(recfile.TestPlan);
                    sw.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    Plan = OpenTap.TestPlan.Load(ms, recfile.FilePath);
                    return true;
                }
            }
            return false;
        }

        public void RemoveRecoveryfile()
        {
            recStream.Dispose();
            File.Delete($".{Process.GetCurrentProcess().Id}.TuiRecovery");
        }
    }
}
