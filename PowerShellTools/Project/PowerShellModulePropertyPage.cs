using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudioTools.Project;

namespace PowerShellTools.Project
{
    [Guid(GuidList.PowerShellModulePropertiesPageGuid)]
    public class PowerShellModulePropertyPage : CommonPropertyPage
    {
        private readonly PowerShellModulePropertyPageControl _control;

        public PowerShellModulePropertyPage()
        {
            _control = new PowerShellModulePropertyPageControl(this);
        }

        public override Control Control
        {
            get { return _control; }
        }

        public override void Apply()
        {
            Project.SetProjectProperty("ManifestFileName", _control.ManifestFileName);
            Project.SetProjectProperty("AliasesToExport", _control.AliasesToExport);
            Project.SetProjectProperty("Author", _control.Author);
            Project.SetProjectProperty("ClrVersion", _control.ClrVersion);
            Project.SetProjectProperty("CmdletsToExport", _control.CmdletsToExport);
            Project.SetProjectProperty("CompanyName", _control.Company);
            Project.SetProjectProperty("Copyright", _control.Copyright);
            Project.SetProjectProperty("Description", _control.Description);
            //Project.SetProjectProperty("DotNetFrameworkVersion", _control.);
            Project.SetProjectProperty("FormatsToProcess", _control.FormatsToProcess);
            Project.SetProjectProperty("FunctionsToProcess", _control.FunctionsToProcess);
            Project.SetProjectProperty("Guid", _control.Guid);
            Project.SetProjectProperty("ModuleList", _control.ModuleList);
            Project.SetProjectProperty("ModuleToProcess", _control.ModulesToProcess);
            Project.SetProjectProperty("Version", _control.Version);
            Project.SetProjectProperty("NestedModules", _control.NestedModules);
            //Project.SetProjectProperty("PowerShellHostName", _control.PowerShellHostVersion);
            Project.SetProjectProperty("PowerShellHostVersion", _control.PowerShellHostVersion);
            Project.SetProjectProperty("PowerShellVersion", _control.PowerShellVersion);
            Project.SetProjectProperty("ProcessorArchitecture", _control.ProcessorArchitecture);
            Project.SetProjectProperty("RequiredAssemblies", _control.RequiredAssemblies);
            Project.SetProjectProperty("RequiredModules", _control.RequiredModules);
            Project.SetProjectProperty("ScriptsToProcess", _control.ScriptsToProcess);
            Project.SetProjectProperty("TypesToProcess", _control.TypesToProcess);
            Project.SetProjectProperty("VariablesToExport", _control.VariablesToExport);
            IsDirty = false;
        }

        public override void LoadSettings()
        {
            _control.LoadingSettings = true;
            _control.ManifestFileName = Project.GetProjectProperty("ManifestFileName");

            _control.AliasesToExport = Project.GetProjectProperty("AliasesToExport", true);
            _control.Author = Project.GetProjectProperty("Author", true);
            _control.ClrVersion = Project.GetProjectProperty("ClrVersion", true);
            _control.CmdletsToExport = Project.GetProjectProperty("CmdletsToExport", true);
            _control.Company = Project.GetProjectProperty("CompanyName", true);
            _control.Copyright = Project.GetProjectProperty("Copyright", true);
            _control.Description = Project.GetProjectProperty("Description", true);
            //_dotNetFrameworkVersion = Project.GetProjectProperty("DotNetFrameworkVersion", true);
            _control.FormatsToProcess = Project.GetProjectProperty("FormatsToProcess", true);
            _control.FunctionsToProcess = Project.GetProjectProperty("FunctionsToProcess", true);
            _control.Guid = Project.GetProjectProperty("Guid", true);
            _control.ModuleList = Project.GetProjectProperty("ModuleList", true);
            _control.ModulesToProcess = Project.GetProjectProperty("ModuleToProcess", true);
            _control.Version = Project.GetProjectProperty("Version", true);
            _control.NestedModules = Project.GetProjectProperty("NestedModules", true);
            //_control.Power = Project.GetProjectProperty("PowerShellHostName", true);
            _control.PowerShellHostVersion = Project.GetProjectProperty("PowerShellHostVersion", true);
            _control.PowerShellVersion = Project.GetProjectProperty("PowerShellVersion", true);
            _control.ProcessorArchitecture = Project.GetProjectProperty("ProcessorArchitecture", true);
            _control.RequiredAssemblies = Project.GetProjectProperty("RequiredAssemblies", true);
            _control.RequiredModules = Project.GetProjectProperty("RequiredModules", true);
            _control.ScriptsToProcess = Project.GetProjectProperty("ScriptsToProcess", true);
            _control.TypesToProcess = Project.GetProjectProperty("TypesToProcess", true);
            _control.VariablesToExport = Project.GetProjectProperty("VariablesToExport", true);
            _control.LoadingSettings = false;
        }

        public override string Name
        {
            get { return "Module"; }
        }
    }
}
