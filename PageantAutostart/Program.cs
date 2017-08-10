using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Newtonsoft.Json;

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
         * 6 No Passphrase Provided
         */
        private static int LoadKey(string key, string pass = null) {
            Process p = Process.Start(key);
            if(p == null || p.HasExited || p.ProcessName != Settings.PageantProcessName)
                return 1;
            for(byte cnt = 0; cnt <= 40 && p.MainWindowHandle.ToInt32() == 0; cnt++)
                Thread.Sleep(250);
            if(p.MainWindowHandle.ToInt32() == 0)
                return 2;
            else if(pass == null)
                return 6;
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
            int failedKeys = 0;
            Config config = new Config();
            ApplyArgs(ParseArgs(args));
            Console.Title = Settings.ConsoleTitle;
            Console.WriteLine(Messages.Launch);
            if(File.Exists(Settings.ConfigFile)) {
                try {
                    config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Settings.ConfigFile));
                    Console.WriteLine("Loaded Config: " + Settings.ConfigFile);
                }
                catch(JsonReaderException) {
                    PrintError("Failed to load config: " + Settings.ConfigFile);
                    Console.WriteLine("Falling back to default config.");
                }
            }
            else
                Console.WriteLine("No config file, loading defaults.");
            Console.WriteLine();
            foreach(KeyConfig kc in config.keys) {
                bool error = false;
                string path = Environment.ExpandEnvironmentVariables(kc.path);
                if(File.Exists(path))
                    switch(LoadKey(path, kc.pass)) {
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
                        case 6:
                            PrintError(Messages.NoPassphraseProvided);
                            error = true;
                            break;
                    }
                else {
                    PrintError(Messages.KeyNotFound);
                    error = true;
                }
                if(error) {
                    PrintError("while loading Key " + kc.path);
                    Console.WriteLine();
                    failedKeys++;
                }
            }
            Console.WriteLine("Failed to load " + Convert.ToString(failedKeys) + " Keys, " + Convert.ToString(config.keys.Length - failedKeys) + " succeeded!");
            Console.WriteLine("Exit in 3 seconds...");
            Thread.Sleep(3000);
            return failedKeys;
        }
    }
}
