using System;
using System.Diagnostics;
using System.IO;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher
{
    public class AppStarter
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppStarter));

        private readonly App _app;

        public AppFinder AppFinder { get; set; }

        public AppStarter(App app)
        {
            Checks.ArgumentNotNull(app, "app");

            DebugLogger.LogConstructor();

            _app = app;
            AppFinder = new AppFinder();
        }

        public void Start()
        {
            DebugLogger.Log("Starting application.");

            PlatformType platformType = Platform.GetPlatformType();
            string appFilePath = AppFinder.FindExecutable(_app.LocalDirectory.Path, platformType);
            if (appFilePath == null)
            {
                throw new InvalidOperationException("Couldn't find executable.");
            }

            DebugLogger.Log(string.Format("Found executable {0}", appFilePath));

            if (NeedPermissionFix(platformType))
            {
                foreach (var fileName in _app.LocalMetaData.GetRegisteredEntries())
                {
                    string filePath = _app.LocalDirectory.Path.PathCombine(fileName);
                    if (IsExecutable(filePath, platformType))
                    {
                        Chmod(filePath, "+x");
                    }
                }
            }

            var processStartInfo = GetProcessStartInfo(appFilePath, platformType);
            StartAppProcess(processStartInfo);
        }

        private bool NeedPermissionFix(PlatformType platformType)
        {
            return platformType == PlatformType.OSX || platformType == PlatformType.Linux;
        }

        private bool IsExecutable(string filePath, PlatformType platformType)
        {
            switch (platformType)
            {
                case PlatformType.Unknown:
                    throw new ArgumentException("Unknown");
                case PlatformType.Windows:
                    throw new ArgumentException("Unsupported");
                case PlatformType.OSX:
                    return MagicBytes.IsMacExecutable(filePath);
                case PlatformType.Linux:
                    return MagicBytes.IsLinuxExecutable(filePath);
                default:
                    throw new ArgumentOutOfRangeException("platformType", platformType, null);
            }
        }

        private ProcessStartInfo GetProcessStartInfo(string executablePath, PlatformType platform)
        {
            string workingDir = Path.GetDirectoryName(executablePath) ?? string.Empty;
            switch (platform)
            {
                case PlatformType.Unknown:
                    throw new ArgumentException("Unknown");;
                case PlatformType.Windows:
                    return new ProcessStartInfo
                    {
                        FileName = executablePath,
                        WorkingDirectory = workingDir
                    };
                case PlatformType.OSX:
                    return new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = string.Format("\"{0}\"", executablePath),
                        WorkingDirectory = workingDir
                    };
                case PlatformType.Linux:
                    return new ProcessStartInfo
                    {
                        FileName = executablePath,
                        WorkingDirectory = workingDir
                    };
                default:
                    throw new ArgumentOutOfRangeException("platform", platform, null);
            }
        }

        private void Chmod(string filePath, string permissions)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "/bin/chmod",
                    Arguments = string.Format("{0} \"{1}\"", permissions, filePath)
                }
            };

            DebugLogger.Log(string.Format("Executing {0} {1}...", process.StartInfo.FileName,
                process.StartInfo.Arguments));

            process.Start();
            process.WaitForExit();

            DebugLogger.Log("Done");
        }

        private void StartAppProcess(ProcessStartInfo processStartInfo)
        {
            DebugLogger.Log(string.Format("Starting process '{0}' with arguments '{1}'", processStartInfo.FileName,
                processStartInfo.Arguments));

            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                DebugLogger.LogError(string.Format("Failed to start process {0}", processStartInfo.FileName));
            }
            else if (process.HasExited)
            {
                DebugLogger.LogError(string.Format("Process '{0}' prematurely exited with code '{1}'",
                    processStartInfo.FileName, process.ExitCode));
            }
        }
    }
}