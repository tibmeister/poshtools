using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools.Project;
using PowerShellTools.Classification;
using PowerShellTools.Common.Logging;
using PowerShellTools.Project.PropertyPages;

namespace PowerShellTools.Project
{
    internal class PowerShellProjectNode : CommonProjectNode, IVsReferenceManagerUser
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (PowerShellProjectNode));

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

            node.OleServiceProvider.AddService(typeof(SVSMDCodeDomProvider), CreateServices, false);

            return node;
        }

        public override CommonFileNode CreateNonCodeFileNode(ProjectElement item)
        {
            var node = new PowerShellFileNode(this, item);
            node.OleServiceProvider.AddService(typeof(SVSMDCodeDomProvider), CreateServices, false);

            return node;
        }

        public override int ImageIndex
        {
            get
            {
                return CommonProjectNode.ImageOffset + (int)ImageListIndex.Project;
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

        public override int AddProjectReference()
        {
            var referenceManager = this.GetService(typeof(SVsReferenceManager)) as IVsReferenceManager;
            if (referenceManager != null)
            {
                referenceManager.ShowReferenceManager(
                    pRefMgrUser: this,
                    lpszDlgTitle: SR.GetString(SR.AddReferenceDialogTitle, CultureInfo.CurrentUICulture),
                    lpszHelpTopic: "VS.ReferenceManager",
                    guidDefaultProviderContext: Common.Constants.ModuleReferenceProvider_guid,
                    fForceShowDefaultProvider: false);

                return VSConstants.S_OK;
            }

            return VSConstants.E_FAIL;
        }

        protected internal override void ProcessReferences()
        {
            base.ProcessReferences();

            foreach (var reference in GetReferenceContainer().EnumReferences())
            {
                Log.DebugFormat("Reference: {0} - {1}", reference.Caption, reference.Url);
            }
        }

        public void ChangeReferences(uint operation, IVsReferenceProviderContext changedContext)
        {
            foreach (var reference in changedContext.References)
            {
                Log.DebugFormat("Reference changed: {0} {1}", reference, reference.GetType());
                changedContext.AddReference(reference as IVsReference);
            }
        }

        public Array GetProviderContexts()
        {
            var targetFrameworkAttribute = Attribute
             .GetCustomAttributes(this.GetType().Assembly, typeof(TargetFrameworkAttribute))
             .OfType<TargetFrameworkAttribute>()
             .FirstOrDefault();
            if (targetFrameworkAttribute != null)
            {
                var referenceManager = this.GetService(typeof(SVsReferenceManager)) as IVsReferenceManager;
                var assemblyContext = referenceManager.CreateProviderContext(VSConstants.AssemblyReferenceProvider_Guid) as IVsAssemblyReferenceProviderContext;
                assemblyContext.TargetFrameworkMoniker = targetFrameworkAttribute.FrameworkName;
                assemblyContext.Tabs = (uint)__VSASSEMBLYPROVIDERTAB.TAB_ASSEMBLY_FRAMEWORK;
                assemblyContext.AssemblySearchPaths = this.GetProjectProperty("AssemblySearchPaths");

                foreach (var node in GetReferenceContainer().EnumReferences().OfType<AssemblyReferenceNode>())
                {
                    var reference = assemblyContext.CreateReference() as IVsAssemblyReference;
                    reference.Name = node.Caption;
                    reference.FullPath = node.Url;
                }

                var moduleContext = referenceManager.CreateProviderContext(Common.Constants.ModuleReferenceProvider_guid) as IVsAssemblyReferenceProviderContext;
                
                return new[] { assemblyContext, moduleContext };
            }

            throw new InvalidOperationException("Reference Manager Failed");
        }
    }
}
