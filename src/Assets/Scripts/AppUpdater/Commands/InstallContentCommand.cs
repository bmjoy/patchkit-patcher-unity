﻿using System.IO;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class InstallContentCommand : BaseAppUpdaterCommand, IInstallContentCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallContentCommand));

        private readonly int _versionId;
        private readonly App _app;

        private string _packagePath;
        private AppContentSummary _versionSummary;
        private IGeneralStatusReporter _copyFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallContentCommand(int versionId, App app)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            AssertChecks.ArgumentNotNull(app, "app");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            _versionId = versionId;
            _app = app;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Installing content.");

            AssertChecks.IsTrue(_app.LocalData.MetaData.GetFileNames().Length == 0, "Cannot install content if previous version is still present.");

            using (var packageDir = new TemporaryDirectory(_app.LocalData.TemporaryData.GetUniquePath()))
            {
                DebugLogger.Log("Unarchiving package.");

                var unarchiver = new Unarchiver(_packagePath, packageDir.Path);

                unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount) =>
                {
                    _unarchivePackageStatusReporter.OnProgressChanged(entry / (double)amount);
                };

                unarchiver.Unarchive(cancellationToken);

                DebugLogger.Log("Copying files.");

                for (int i = 0; i < _versionSummary.Files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InstallFile(_versionSummary.Files[i].Path, packageDir.Path);

                    _copyFilesStatusReporter.OnProgressChanged((i + 1)/(double) _versionSummary.Files.Length);
                }
            }
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing content installation.");

            _app.LocalData.EnableWriteAccess();

            _versionSummary = _app.RemoteData.MetaData.GetContentSummary(_versionId);

            double copyFilesWeight = StatusWeightHelper.GetCopyContentFilesWeight(_versionSummary);
            _copyFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(copyFilesWeight);

            double unarchivePackageWeight = StatusWeightHelper.GetUnarchivePackageWeight(_versionSummary.Size);
            _unarchivePackageStatusReporter = statusMonitor.CreateGeneralStatusReporter(unarchivePackageWeight);
        }

        public void SetPackagePath(string packagePath)
        {
            _packagePath = packagePath;
        }

        private void InstallFile(string fileName, string packageDirPath)
        {
            DebugLogger.Log(string.Format("Installing file {0}", fileName));

            string sourceFilePath = Path.Combine(packageDirPath, fileName);

            if (!File.Exists(sourceFilePath))
            {
                throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
            }

            _app.LocalData.CreateOrUpdateFile(fileName, sourceFilePath);
            _app.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
        }
    }
}