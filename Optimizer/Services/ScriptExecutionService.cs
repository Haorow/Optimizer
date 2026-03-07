using System;
using System.Diagnostics;
using System.IO;
using Optimizer.Models;
using Optimizer.Services.ScriptGeneration;
using Optimizer.Helpers;

namespace Optimizer.Services
{
    /// <summary>
    /// Service de gestion de l'exécution des scripts AutoHotkey.
    /// </summary>
    public class ScriptExecutionService : IDisposable
    {
        private Process? _mcProcess;
        private Process? _hcProcess;
        private Process? _wsProcess;
        private Process? _etProcess;

        private readonly string _ahkExePath;
        private readonly string _scriptsDirectory;
        private readonly JobObject _jobObject;

        private bool _disposed = false;

        public ScriptExecutionService()
        {
            _ahkExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Binaries", "AutoHotkey64.exe");
            _scriptsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");

            if (!Directory.Exists(_scriptsDirectory))
                Directory.CreateDirectory(_scriptsDirectory);

            _jobObject = new JobObject();
        }

        #region Vérification AutoHotkey

        /// <summary>
        /// Vérifie que AutoHotkey64.exe est présent au démarrage.
        /// Retourne un message d'erreur clair si absent, null si tout est OK.
        /// À appeler depuis MainViewModel au démarrage avant toute exécution de script.
        /// </summary>
        public string? ValidateAutoHotkeyExists()
        {
            if (!File.Exists(_ahkExePath))
                return $"AutoHotkey64.exe est introuvable.\n\nChemin attendu :\n{_ahkExePath}\n\nLes fonctionnalités Mouse Clone, Hotkey Clone et Window Switcher seront indisponibles.";

            return null;
        }

        #endregion

        #region Mouse Clone

        public void StartMouseClone(AhkData ahkData)
        {
            StopScript(ref _mcProcess);
            ScriptGenerator.Generate_MC_Script(ahkData);
            StartScript("MouseClone", ref _mcProcess);
        }

        public void StopMouseClone() => StopScript(ref _mcProcess);

        #endregion

        #region Hotkey Clone

        public void StartHotkeyClone(AhkData ahkData)
        {
            StopScript(ref _hcProcess);
            ScriptGenerator.Generate_HC_Script(ahkData);
            StartScript("HotkeyClone", ref _hcProcess);
        }

        public void StopHotkeyClone() => StopScript(ref _hcProcess);

        #endregion

        #region Window Switcher

        public void StartWindowSwitcher(AhkData ahkData)
        {
            StopScript(ref _wsProcess);
            ScriptGenerator.Generate_WS_Script(ahkData);
            StartScript("WindowSwitcher", ref _wsProcess);
        }

        public void StopWindowSwitcher() => StopScript(ref _wsProcess);

        #endregion

        #region Easy Team

        public void ExecuteEasyTeam(AhkData ahkData)
        {
            StopScript(ref _etProcess);
            ScriptGenerator.Generate_ET_Script(ahkData);
            StartScript("EasyTeam", ref _etProcess);
        }

        public void UpdateEasyTeamScript(AhkData ahkData)
        {
            ScriptGenerator.Generate_ET_Script(ahkData);
        }

        #endregion

        #region Gestion des processus

        private void StartScript(string scriptName, ref Process? process)
        {
            string scriptPath = Path.Combine(_scriptsDirectory, $"{scriptName}.ahk");

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException($"Le script {scriptPath} n'existe pas.");

            if (!File.Exists(_ahkExePath))
                throw new FileNotFoundException($"AutoHotkey64.exe est introuvable : {_ahkExePath}");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _ahkExePath,
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process = Process.Start(startInfo);

                if (process != null)
                {
                    _jobObject.AddProcess(process.Handle);
                    Logger.Log($"{scriptName} activé !");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de l'exécution d'AutoHotkey : {ex.Message}", ex);
            }
        }

        private void StopScript(ref Process? process)
        {
            if (process != null && !process.HasExited)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(1000);
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Log($"Erreur lors de l'arrêt du script : {ex.Message}");
                }
                finally
                {
                    process = null;
                }
            }
        }

        public void StopAllScripts()
        {
            StopScript(ref _mcProcess);
            StopScript(ref _hcProcess);
            StopScript(ref _wsProcess);
            StopScript(ref _etProcess);
            KillAllAutoHotkeyProcesses();
        }

        private void KillAllAutoHotkeyProcesses()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("AutoHotkey64"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Erreur lors de la fermeture du processus {process.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Erreur lors de la recherche des scripts actifs: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                StopAllScripts();
                _jobObject.Dispose();
            }

            _disposed = true;
        }

        ~ScriptExecutionService()
        {
            Dispose(false);
        }

        #endregion

        #region Job Object

        /// <summary>
        /// Lie les processus enfants au parent via un Job Object Windows.
        /// Si le parent meurt, les processus enfants sont automatiquement tués.
        /// </summary>
        internal class JobObject : IDisposable
        {
            [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? name);

            [System.Runtime.InteropServices.DllImport("kernel32.dll")]
            static extern bool SetInformationJobObject(IntPtr job, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
            static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

            [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
            static extern bool CloseHandle(IntPtr handle);

            private IntPtr _handle;
            private bool _disposed = false;

            public JobObject()
            {
                _handle = CreateJobObject(IntPtr.Zero, null);

                var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
                {
                    BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                    {
                        LimitFlags = 0x2000 // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                    }
                };

                int length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(length);
                System.Runtime.InteropServices.Marshal.StructureToPtr(extendedInfo, ptr, false);

                if (!SetInformationJobObject(_handle, JobObjectInfoType.ExtendedLimitInformation, ptr, (uint)length))
                    throw new InvalidOperationException("Impossible de configurer le Job Object");

                System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            }

            public void AddProcess(IntPtr processHandle)
                => AssignProcessToJobObject(_handle, processHandle);

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed) return;

                if (_handle != IntPtr.Zero)
                {
                    CloseHandle(_handle);
                    _handle = IntPtr.Zero;
                }

                _disposed = true;
            }

            ~JobObject() => Dispose(false);

            #region Structures

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            struct JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                public Int64 PerProcessUserTimeLimit;
                public Int64 PerJobUserTimeLimit;
                public uint LimitFlags;
                public UIntPtr MinimumWorkingSetSize;
                public UIntPtr MaximumWorkingSetSize;
                public uint ActiveProcessLimit;
                public UIntPtr Affinity;
                public uint PriorityClass;
                public uint SchedulingClass;
            }

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            struct IO_COUNTERS
            {
                public UInt64 ReadOperationCount;
                public UInt64 WriteOperationCount;
                public UInt64 OtherOperationCount;
                public UInt64 ReadTransferCount;
                public UInt64 WriteTransferCount;
                public UInt64 OtherTransferCount;
            }

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
                public IO_COUNTERS IoInfo;
                public UIntPtr ProcessMemoryLimit;
                public UIntPtr JobMemoryLimit;
                public UIntPtr PeakProcessMemoryUsed;
                public UIntPtr PeakJobMemoryUsed;
            }

            enum JobObjectInfoType
            {
                AssociateCompletionPortInformation = 7,
                BasicLimitInformation = 2,
                BasicUIRestrictions = 4,
                EndOfJobTimeInformation = 6,
                ExtendedLimitInformation = 9,
                SecurityLimitInformation = 5,
                GroupInformation = 11
            }

            #endregion
        }

        #endregion
    }
}