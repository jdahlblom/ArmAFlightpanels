﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using ArmAFlightpanels.Properties;

namespace ArmAFlightpanels
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex;
        private bool _hasHandle;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                //ArmAFlightpanels.exe OpenProfile="C:\Users\User\Documents\Spitfire_Saitek_DCS_Profile.bindings"
                //ArmAFlightpanels.exe OpenProfile='C:\Users\User\Documents\Spitfire_Saitek_DCS_Profile.bindings'

                //1 Check for start arguments.
                //2 If argument and profile exists close running instance, start this with profile chosen
                var closeCurrentInstance = false;

                try
                {
                    if (e.Args.Length > 0 && e.Args[0].Contains("OpenProfile") && e.Args[0].Contains("="))
                    {
                        var array = e.Args[0].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        if (array[0].Equals("OpenProfile") && File.Exists(array[1].Replace("\"", "").Replace("'", "")))
                        {
                            Settings.Default.LastProfileFileUsed = array[1].Replace("\"", "").Replace("'","");
                            Settings.Default.RunMinimized = true;
                            closeCurrentInstance = true;
                        }
                        else
                        {
                            MessageBox.Show("Invalid startup arguments." + Environment.NewLine + array[0] + Environment.NewLine + array[1]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error processing startup arguments." + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
                }

                // get application GUID as defined in AssemblyInfo.cs
                var appGuid = "{23DB8D4F-D76E-4DF4-B04F-4F4EB0A8E992}";

                // unique id for global mutex - Global prefix means it is global to the machine
                string mutexId = "Global\\" + appGuid;

                // Need a place to store a return value in Mutex() constructor call
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow);
                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);

                _mutex = new Mutex(false, mutexId,  out var createdNew, securitySettings);

                _hasHandle = false;
                try
                {
                    _hasHandle = _mutex.WaitOne(2000, false);
                }
                catch (AbandonedMutexException)
                {
                    // Log the fact that the mutex was abandoned in another process,
                    // it will still get acquired
                    //_hasHandle = true;
                }
                
                if (!closeCurrentInstance && !_hasHandle)
                {
                    MessageBox.Show("ArmAFlightpanels is already running..");
                    Current.Shutdown(0);
                    Environment.Exit(0);
                }
                if (closeCurrentInstance && !_hasHandle)
                {
                    foreach (var process in Process.GetProcesses())
                    {
                        if (process.ProcessName.Equals(Process.GetCurrentProcess().ProcessName) && process.Id != Process.GetCurrentProcess().Id)
                        {
                            process.Kill();
                            break;
                        }
                    }
                    // Wait for process to close
                    Thread.Sleep(2000);
                }
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error starting ArmAFlightpanels." + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
                Current.Shutdown(0);
                Environment.Exit(0);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_hasHandle)
            {
                _mutex?.ReleaseMutex();
            }
            base.OnExit(e);
        }
    }
}
