using System;
using System.Windows.Forms;
using Microsoft.VisualStudioTools.Project;

namespace PowerShellTools.Project
{
    public partial class PowerShellModulePropertyPageControl : UserControl
    {
        private readonly CommonPropertyPage _page;

        public bool LoadingSettings { get; set; }

        public PowerShellModulePropertyPageControl(CommonPropertyPage propertyPage)
        {
            InitializeComponent();
            _page = propertyPage;
            txtManifestFileName.TextChanged += Changed;
            txtAuthor.TextChanged += Changed;
            txtAlisesToExport.TextChanged += Changed;
            cmoCLRVersion.SelectedIndexChanged += Changed;
            txtCmdletsToExport.TextChanged += Changed;
            txtCompany.TextChanged += Changed;
            txtCopyright.TextChanged += Changed;
            txtDescription.TextChanged += Changed;
            txtFormatsToProcess.TextChanged += Changed;
            txtFunctionsToProcess.TextChanged += Changed;
            txtGuid.TextChanged += Changed;
            txtModuleList.TextChanged += Changed;
            txtModuleToProcess.TextChanged += Changed;
            txtNestedModules.TextChanged += Changed;
            txtPowerShellHostVersion.TextChanged += Changed;
            cmoPowerShellVersion.SelectedIndexChanged += Changed;
            cmoProcessorArchitecture.SelectedIndexChanged += Changed;
            txtRequiredModules.TextChanged += Changed;
            txtRequiredAssemblies.TextChanged += Changed;
            txtScriptsToProcess.TextChanged += Changed;
            txtTypesToProcess.TextChanged += Changed;
            txtVariablesToExport.TextChanged += Changed;
            txtVersion.TextChanged += Changed;
        }

        void Changed(object sender, EventArgs e)
        {
            if (!LoadingSettings)
                _page.IsDirty = true;
        }

        public string ManifestFileName
        {
            get { return txtManifestFileName.Text; }
            set { txtManifestFileName.Text = value; }
        }

        public string Author
        {
            get { return txtAuthor.Text; }
            set { txtAuthor.Text = value; }
        }

        public string AliasesToExport
        {
            get { return txtAlisesToExport.Text; }
            set { txtAlisesToExport.Text = value; }
        }

        public string ClrVersion
        {
            get { return cmoCLRVersion.SelectedText; }
            set { cmoCLRVersion.SelectedText = value; }
        }

        public string CmdletsToExport
        {
            get { return txtCmdletsToExport.Text; }
            set { txtCmdletsToExport.Text = value; }
        }

        public string Company
        {
            get { return txtCompany.Text;  }
            set { txtCompany.Text = value; }
        }

        public string Copyright
        {
            get { return txtCopyright.Text; }
            set { txtCopyright.Text = value; }
        }

        public string Description
        {
            get { return txtDescription.Text; }
            set { txtDescription.Text = value; }
        }

        public string FormatsToProcess
        {
            get { return txtFormatsToProcess.Text; }
            set { txtFormatsToProcess.Text = value; }
        }

        public string FunctionsToProcess
        {
            get { return txtFunctionsToProcess.Text; }
            set { txtFunctionsToProcess.Text = value; }
        }

        public string Guid
        {
            get { return txtGuid.Text; }
            set { txtGuid.Text = value; }
        }

        public string ModuleList
        {
            get { return txtModuleList.Text; }
            set { txtModuleList.Text = value; }
        }

        public string ModulesToProcess
        {
            get { return txtModuleToProcess.Text; }
            set { txtModuleToProcess.Text = value; }
        }

        public string NestedModules
        {
            get { return txtNestedModules.Text; }
            set { txtNestedModules.Text = value; }
        }

        public string PowerShellHostVersion
        {
            get { return txtPowerShellHostVersion.Text; }
            set { txtPowerShellHostVersion.Text = value; }
        }

        public string PowerShellVersion
        {
            get { return cmoPowerShellVersion.SelectedText; }
            set { cmoPowerShellVersion.SelectedText = value; }
        }

        public string ProcessorArchitecture
        {
            get { return cmoProcessorArchitecture.SelectedText; }
            set { cmoProcessorArchitecture.SelectedText = value; }
        }

        public string RequiredAssemblies
        {
            get { return txtRequiredAssemblies.Text; }
            set { txtRequiredAssemblies.Text = value; }
        }

        public string RequiredModules
        {
            get { return txtRequiredModules.Text; }
            set { txtRequiredModules.Text = value; }
        }

        public string ScriptsToProcess
        {
            get { return txtScriptsToProcess.Text; }
            set { txtScriptsToProcess.Text = value; }
        }

        public string TypesToProcess
        {
            get { return txtTypesToProcess.Text; }
            set { txtTypesToProcess.Text = value; }
        }

        public string VariablesToExport
        {
            get { return txtVariablesToExport.Text; }
            set { txtVariablesToExport.Text = value; }
        }

        public string Version
        {
            get { return txtVersion.Text; }
            set { txtVersion.Text = value; }
        }

        private void PowerShellModulePropertyPageControl_Load(object sender, EventArgs e)
        {

        }

        private void btnGuid_Click(object sender, EventArgs e)
        {
            txtGuid.Text = System.Guid.NewGuid().ToString();
        }
}


}
