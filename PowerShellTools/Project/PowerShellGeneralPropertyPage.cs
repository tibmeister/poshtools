using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudioTools.Project;

namespace PowerShellTools.Project
{
    [Guid(GuidList.PowerShellGeneralPropertiesPageGuid)]
    public class PowerShellGeneralPropertyPage : CommonPropertyPage
    {
        private PowerShellProjectNode _project;
        private readonly PowerShellGeneralPropertyPageControl _control;

        public PowerShellGeneralPropertyPage()
        {
            _control = new PowerShellGeneralPropertyPageControl(this);
        }

        internal override CommonProjectNode Project
        {
            get { return _project;  }
            set { _project = (PowerShellProjectNode)value; }
        }

        public override Control Control
        {
            get
            {
                return _control;
            }
        }

        public override void Apply()
        {
            Project.SetProjectProperty(ProjectConstants.CodeSigningCert, _control.CodeSigningCert);
            Project.SetProjectProperty(ProjectConstants.OutputDirectory, _control.OutputDirectory);
            Project.SetProjectProperty(ProjectConstants.SignOutput, _control.SignOutput.ToString());
            IsDirty = false;
        }

        public override void LoadSettings()
        {
            _control.LoadingSettings = true;
            _control.CodeSigningCert = Project.GetProjectProperty(ProjectConstants.CodeSigningCert, false);
            _control.SignOutput = Convert.ToBoolean(Project.GetProjectProperty(ProjectConstants.SignOutput, false));
            _control.OutputDirectory = Project.GetProjectProperty(ProjectConstants.OutputDirectory, false);
            _control.LoadingSettings = false;
        }

        public override string Name
        {
            get { return "General"; }
        }
    }
}
