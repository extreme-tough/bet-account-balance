using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

using Utility.ModifyRegistry;

namespace _30SiteBalance
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //ModifyRegistry objReg = new ModifyRegistry();
            //objReg.ShowError = true;
            //objReg.BaseRegistryKey = Registry.CurrentUser;            
            //objReg.SubKey = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Zones\\3";
            //objReg.Write("1400", 3);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form1 frmScrap = new Form1();
            Process.Start("kldlg.exe");

            if (args.Length>=1 && args[0].ToUpper() == "/AUTO")
            {
                frmScrap.Autopilot = true;
                frmScrap.StartProcess();
            }
            else
            {
                frmScrap.Autopilot = false;
                Application.Run(frmScrap);
            }
            KillProcess("KillDialogs");
            //objReg.BaseRegistryKey = Registry.CurrentUser;            
            //objReg.SubKey = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Internet Settings\\Zones\\3";
            //objReg.Write("1400", 0);
        }

        static private void KillProcess(string processName)
        {
            System.Diagnostics.Process myproc = new System.Diagnostics.Process();
            //Get all instances of proc that are open, attempt to close them.

            try
            {
                foreach (Process thisproc in Process.GetProcessesByName(processName))
                {
                    if (thisproc.Id != System.Diagnostics.Process.GetCurrentProcess().Id)
                    {
                        if (!thisproc.CloseMainWindow())
                        {
                            //If closing is not successful or no desktop window handle, then force termination.
                            thisproc.Kill();
                        }
                    }
                } // next proc
            }
            catch 
            {

            }
        }
    }
}
