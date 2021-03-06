﻿using Shapeshifter.WindowsDesktop.Services.Processes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

using Shapeshifter.WindowsDesktop.Infrastructure.Events;

namespace Shapeshifter.WindowsDesktop.Services.Processes
{
    using System.Diagnostics;
    using System.Management;

    class ProcessWatcher : IProcessWatcher
    {
        readonly List<string> _processNamesToWatch;

        ManagementEventWatcher _watcher;

        public event EventHandler<ProcessStartedEventArgument> ProcessStarted;

        const int ProcessPollingIntervalInSeconds = 3;

        public IReadOnlyList<string> ProcessNamesToWatch
            => new List<string>(_processNamesToWatch);

        public ProcessWatcher()
        {
            _processNamesToWatch = new List<string>();
        }

        public void AddProcessNameToWatchList(string processName)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("Can't add items to the list of watched processes when the watcher is connected.");
            }
            
            _processNamesToWatch.Add(
                RemoveExtensionFromProcessName(processName));
        }

        static string RemoveExtensionFromProcessName(string processName)
        {
            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }
            return processName;
        }

        public void RemoveProcessNameFromWatchList(string processName)
        {
            if (IsConnected)
            {
                throw new InvalidOperationException("Can't remove items from the list of watched processes when the watcher is connected.");
            }

            _processNamesToWatch.Remove(
                RemoveExtensionFromProcessName(processName));
        }

        public bool IsConnected { get; private set; }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException(
                    "The process watcher is already disconnected.");
            }

            _watcher.EventArrived -= WatcherProcessStarted;
            _watcher.Stop();
            _watcher = null;

            IsConnected = false;
        }

        public void Connect()
        {
            if (IsConnected)
            {
                throw new InvalidOperationException(
                    "The process watcher is already connected.");
            }

            SetupFutureProcessMonitoring();
            ScanCurrentProcesses();

            IsConnected = true;
        }

        void ScanCurrentProcesses()
        {
            foreach (var processName in _processNamesToWatch)
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    FireOnProcessStarted(
                        $"{process.ProcessName}.exe",
                        process.Id);
                }
            }
        }

        void SetupFutureProcessMonitoring()
        {
            var querySuffix = string.Empty;
            if (_processNamesToWatch.Any())
            {
                querySuffix += " AND (";
                for (var i = 0; i < _processNamesToWatch.Count; i++)
                {
                    var processName = _processNamesToWatch[i];
                    if (i > 0)
                    {
                        querySuffix += " OR ";
                    }
                    querySuffix += " TargetInstance.Name = '" + processName + ".exe'";
                }
                querySuffix += ")";
            }

            var queryString =
                "SELECT *" +
                "  FROM __InstanceCreationEvent " +
                "WITHIN " + ProcessPollingIntervalInSeconds + " " +
                " WHERE TargetInstance ISA 'Win32_Process'" + querySuffix;

            const string scope = @"\\.\root\CIMV2";

            _watcher = new ManagementEventWatcher(scope, queryString);
            _watcher.EventArrived += WatcherProcessStarted;
            _watcher.Start();
        }

        void WatcherProcessStarted(object sender, EventArrivedEventArgs e)
        {
            var targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;

            var processName = targetInstance.Properties["Name"].Value.ToString();
            var processId = int.Parse(targetInstance.Properties["ProcessId"].Value.ToString());

            FireOnProcessStarted(processName, processId);
        }

        void FireOnProcessStarted(string processName, int processId)
        {
            OnProcessStarted(
                new ProcessStartedEventArgument(
                    processName,
                    processId));
        }

        protected virtual void OnProcessStarted(ProcessStartedEventArgument e)
        {
            ProcessStarted?.Invoke(this, e);
        }
    }
}
