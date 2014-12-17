using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudioTools.Project;

namespace PowerShellTools.Project
{
    [Guid(GuidList.PowerShellDebugPropertiesPageGuid)]
    public class PowerShellDebugPropertyPage : CommonPropertyPage
    {
        private readonly PowerShellDebugPropertyPageControl _control;

        public PowerShellDebugPropertyPage()
        {
            _control = new PowerShellDebugPropertyPageControl(this);
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
            Project.SetProjectProperty(ProjectConstants.DebugArguments, _control.Arguments);
            IsDirty = false;
        }

        public override void LoadSettings()
        {
            _control.LoadingSettings = true;
            _control.Arguments = Project.GetProjectProperty(ProjectConstants.DebugArguments);
            _control.LoadingSettings = false;
        }

        public override string Name
        {
            get { return "Debug"; }
        }
    }
}
