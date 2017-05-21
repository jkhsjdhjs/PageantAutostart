using System;
using System.Diagnostics;
using System.Threading;
using JsonConfig;

namespace PageantAutostart {
    static class Program {
        const uint WM_SETTEXT = 0x000C;
        const uint BM_CLICK = 0x00F5;

        private static void PrintError(string error) {
            ConsoleColor c = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ForegroundColor = c;
        }

        private static byte ParseArgs(string[] args) {
            byte rv = 0;
            foreach(string s in args) {
                switch(s) {
                    case "-s":
                        rv += 1;
                        break;
                }
            }
            return rv;
        }

        private static void ApplyArgs(byte a) {
            if(Convert.ToBoolean(a & 1)) {
                IntPtr hwnd = DLL.GetConsoleWindow();
                DLL.ShowWindow(hwnd, 0);
            }
        }

        /* Return Codes:
         * 0 Success
         * 1 PageantLaunchError
         * 2 PageantWindowDidNotAppear
         * 3 TextBoxHandleError
         * 4 PassphraseError
         * 5 ButtonHandleError
         */
        private static int LoadKey(string key, string pass) {
            Process p = Process.Start(Environment.ExpandEnvironmentVariables(key));
            if(p == null || p.HasExited || p.ProcessName != Settings.PageantProcessName)
                return 1;
            for(byte cnt = 0; cnt <= 40 && p.MainWindowHandle.ToInt32() == 0; cnt++)
                Thread.Sleep(250);
            if(p.MainWindowHandle.ToInt32() == 0)
                return 2;
            IntPtr TextBoxHandle = DLL.FindWindowEx(p.MainWindowHandle, IntPtr.Zero, Settings.TextBoxClassName, null);
            if(TextBoxHandle.ToInt32() == 0)
                return 3;
            if(!Convert.ToBoolean(DLL.SendMessage(TextBoxHandle, WM_SETTEXT, IntPtr.Zero, pass)))
                return 4;
            IntPtr OkButtonHandle = DLL.FindWindowEx(p.MainWindowHandle, IntPtr.Zero, Settings.OKBtnClassName, Settings.OKBtnName);
            if(OkButtonHandle.ToInt32() == 0)
                return 5;
            DLL.SendMessage(OkButtonHandle, BM_CLICK, IntPtr.Zero, null);
            return 0;
        }

        public static int Main(string[] args) {
            Config.SetUserConfig(Config.ParseJson("config.json"));
            int failedKeys = 0;
            ApplyArgs(ParseArgs(args));
            Console.Title = Settings.ConsoleTitle;
            Console.WriteLine(Messages.Launch);
            Console.WriteLine();
            string[] keys = new string[] {@"%USERPROFILE%\.ssh\id_rsa.ppk"};
            foreach(string k in keys) {
                bool error = false;
                switch(LoadKey(k, "the respective passphrase")) {
                    case 0:
                        break;
                    case 1:
                        PrintError(Messages.PageantLaunchError);
                        error = true;
                        break;
                    case 2:
                        PrintError(Messages.PageantWindowDidNotAppear);
                        error = true;
                        break;
                    case 3:
                        PrintError(Messages.TextBoxHandleError);
                        error = true;
                        break;
                    case 4:
                        PrintError(Messages.PassphraseError);
                        error = true;
                        break;
                    case 5:
                        PrintError(Messages.ButtonHandleError);
                        error = true;
                        break;

                }
                if(error) {
                    PrintError("while loading Key " + k);
                    Console.WriteLine();
                    failedKeys++;
                }
                
            }
            Console.WriteLine("Failed to load " + Convert.ToString(failedKeys) + " Keys, " + Convert.ToString(keys.Length - failedKeys) + " succeeded!");
            Console.WriteLine("Exit in 3 seconds...");
            Thread.Sleep(3000);
            return failedKeys;
        }
    }
}
