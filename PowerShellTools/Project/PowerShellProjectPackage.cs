using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudioTools.Project;
using PowerShellTools.Classification;

namespace PowerShellTools.Project
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(GuidList.PowerShellToolsProjectPackageGuid)]
    [ProvideProjectFactory(typeof(PowerShellProjectFactory), "PowerShell", "PowerShell Project Files (*.pssproj);*.pssproj", "pssproj", "pssproj", @"\ProjectTemplates\PowerShell", LanguageVsTemplate = "PowerShell", NewProjectRequireNewFolderVsTemplate = false)]
    [ProvideProjectItem(typeof(PowerShellProjectFactory), "PowerShell", @"Templates", 500)]
    [ProvideEditorExtension(typeof(PowerShellEditorFactory), PowerShellConstants.PSD1File, 1000)]
    [ProvideEditorExtension(typeof(PowerShellEditorFactory), PowerShellConstants.PS1File, 1000)]
    [ProvideEditorExtension(typeof(PowerShellEditorFactory), PowerShellConstants.PSM1File, 1000)]
    [ProvideEditorLogicalView(typeof(PowerShellEditorFactory), "{7651a702-06e5-11d1-8ebd-00a0c90f26ea}")]  //LOGVIEWID_Designer
    [ProvideEditorLogicalView(typeof(PowerShellEditorFactory), "{7651a701-06e5-11d1-8ebd-00a0c90f26ea}")]  //LOGVIEWID_Code
    [DeveloperActivity("PowerShell", typeof(PowerShellProjectPackage))]
    [Export]
    public class PowerShellProjectPackage : CommonProjectPackage
    {
        private readonly IDependencyValidator _validator;

        public PowerShellProjectPackage()
        {
            var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            _validator = (IDependencyValidator)componentModel.GetService<IDependencyValidator>();
        }

        public override ProjectFactory CreateProjectFactory()
        {
            return new PowerShellProjectFactory(this, _validator.Validate());
        }

        public override CommonEditorFactory CreateEditorFactory()
        {
            return new PowerShellEditorFactory(this, _validator.Validate());
        }

        public override uint GetIconIdForAboutBox()
        {
            //TODO: GetIconIdForAboutBox
            return 0;
        }

        public override uint GetIconIdForSplashScreen()
        {
            //TODO: GetIconIdFroSplashScreen
            return 0;
        }

        public override string GetProductName()
        {
            return PowerShellConstants.LanguageName;
        }

        public override string GetProductDescription()
        {
            return PowerShellConstants.LanguageName;
        }

        public override string GetProductVersion()
        {
            return this.GetType().Assembly.GetName().Version.ToString();
        }
    }
}
