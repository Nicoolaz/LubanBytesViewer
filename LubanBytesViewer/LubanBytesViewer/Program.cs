using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace LubanBytesViewer
{
    static class Program
    {
        

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            int processorCount = Environment.ProcessorCount;
            ThreadPool.SetMinThreads(Math.Max(4, processorCount), 0);
            ThreadPool.SetMaxThreads(Math.Max(16, processorCount * 2), 2);

            
            //TableDllImporter.ImportDll();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BytesViewer());
        }
    }
}