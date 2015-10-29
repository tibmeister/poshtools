using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools.Project;
using PowerShellTools.Classification;
using PowerShellTools.Project.PropertyPages;

namespace PowerShellTools.Project
{
    internal class PowerShellProjectNode : CommonProjectNode
    {
        private readonly CommonProjectPackage _package;
        private static readonly ImageList ProjectImageList =
            Utilities.GetImageList(
                typeof(PowerShellProjectNode).Assembly.GetManifestResourceStream(
                    "PowerShellTools.Project.Resources.ImageList.bmp"));

        private readonly bool _dependenciesResolved;
        public PowerShellProjectNode(CommonProjectPackage package, bool dependenciesResolved)
            : base(package, ProjectImageList)
        {
            _dependenciesResolved = dependenciesResolved;
            _package = package;
            AddCATIDMapping(typeof(DebugPropertyPage), typeof(DebugPropertyPage).GUID);
            AddCATIDMapping(typeof(InformationPropertyPage), typeof(InformationPropertyPage).GUID);
            AddCATIDMapping(typeof(ComponentsPropertyPage), typeof(ComponentsPropertyPage).GUID);
            AddCATIDMapping(typeof(ExportsPropertyPage), typeof(ExportsPropertyPage).GUID);
            AddCATIDMapping(typeof(RequirementsPropertyPage), typeof(RequirementsPropertyPage).GUID);
        }

        public override Type GetProjectFactoryType()
        {
            return typeof (PowerShellProjectFactory);
        }

        public override Type GetEditorFactoryType()
        {
            return typeof(PowerShellEditorFactory);
        }

        public override string GetProjectName()
        {
            return "PowerShellProject";
        }

        public override string GetFormatList()
        {
            return "PowerShell Project File (*.pssproj)\n*.pssproj\nAll Files (*.*)\n*.*\n";
        }

        public override Type GetGeneralPropertyPageType()
        {
            return null;
        }

        protected override Guid[] GetConfigurationIndependentPropertyPages()
        {
            return new[] { 
                typeof(DebugPropertyPage).GUID, 
                typeof(InformationPropertyPage).GUID, 
                typeof(ComponentsPropertyPage).GUID, 
                typeof(ExportsPropertyPage).GUID, 
                typeof(RequirementsPropertyPage).GUID };
        }

        public override Type GetLibraryManagerType()
        {
            return typeof(PowerShellLibraryManager);
        }

        public override IProjectLauncher GetLauncher()
        {
            return new PowerShellProjectLauncher(this, _dependenciesResolved);
        }

        protected override Stream ProjectIconsImageStripStream
        {
            get
            {
                return typeof(PowerShellProjectNode).Assembly.GetManifestResourceStream("PowerShellTools.Project.Resources.CommonImageList.bmp");
            }
        }

        public override string[] CodeFileExtensions
        {
            get
            {
                return new[] { PowerShellConstants.PS1File, PowerShellConstants.PSD1File, PowerShellConstants.PSM1File };
            }
        }

        public override CommonFileNode CreateCodeFileNode(ProjectElement item)
        {
            var node = new PowerShellFileNode(this, item);

            //node.OleServiceProvider.AddService(typeof(SVSMDCodeDomProvider), CreateServices, false);

            return node;
        }

        public override CommonFileNode CreateNonCodeFileNode(ProjectElement item)
        {
            var node = new PowerShellFileNode(this, item);
            //node.OleServiceProvider.AddService(typeof(SVSMDCodeDomProvider), CreateServices, false);

            return node;
        }

        internal override string IssueTrackerUrl { get;  }

        public override int ImageIndex
        {
            get
            {
                return (int)ImageListIndex.Project;
            }
        }

        protected override ConfigProvider CreateConfigProvider()
        {
            return new PowerShellConfigProvider(_package, this);
        }

        protected override NodeProperties CreatePropertiesObject()
        {
            return new PowerShellProjectNodeProperties(this);
        }

        /// <summary>
        /// Creates the services exposed by this project.
        /// </summary>
        protected object CreateServices(Type serviceType)
        {
            object service = null;
            if (typeof(SVSMDCodeDomProvider) == serviceType)
            {
                service = new PowerShellCodeDomProvider();
            }

            return service;
        }

        public override bool IsCodeFile(string fileName)
        {
            if (String.IsNullOrEmpty(fileName)) return false;

            var fi = new FileInfo(fileName);

            return CodeFileExtensions.Any(x => x.Equals(fi.Extension, StringComparison.OrdinalIgnoreCase));
        }
    }
}
