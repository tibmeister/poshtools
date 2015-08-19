using System;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using System.ComponentModel.Composition;
using PowerShellTools.Common;

namespace PowerShellTools
{
    [Export(typeof(IDependencyValidator))]
    internal class DependencyValidator : IDependencyValidator
    {
        private static readonly Version RequiredPowerShellVersion = new Version(3, 0);

        private readonly Package _package;

        public DependencyValidator()
        {
            _package = PowerShellToolsPackage.Instance;
        }

        private bool? _previousResult;
        public bool Validate()
        {
            if (_previousResult.HasValue) return _previousResult.Value;
            return ValidateInstalledPowerShellVersion();
        }

        public bool ValidateInstalledPowerShellVersion()
        {
            if (InstalledPowerShellVersion < RequiredPowerShellVersion)
            {
                try
                {
                    if (!VsShellUtilities.IsInAutomationFunction(_package) && 
                        MessageBox.Show(Resources.MissingPowerShellVersion,
                                        Resources.MissingDependency,
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(PowerShellTools.Common.Constants.PowerShellInstallFWLink);
                    }
                }
                catch (InvalidOperationException)
                {
                    
                }



                _previousResult = false;
            }
            else
            {
                _previousResult = true;    
            }

            return _previousResult.Value;
        }

        public static Version InstalledPowerShellVersion
        {
            get
            {
                return DependencyUtilities.GetInstalledPowerShellVersion();
            }
        }
    }
}
