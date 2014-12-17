using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudioTools.Project;

namespace PowerShellTools.Project
{
    public partial class PowerShellDebugPropertyPageControl : UserControl
    {
        private CommonPropertyPage _page;

        public bool LoadingSettings { get; set; }

        public PowerShellDebugPropertyPageControl(CommonPropertyPage page)
        {
            _page = page;
            InitializeComponent();

            txtArguments.TextChanged += txtArguments_TextChanged;
        }

        void txtArguments_TextChanged(object sender, EventArgs e)
        {
            if (!LoadingSettings)
                _page.IsDirty = true;
        }

        public string Arguments
        {
            get { return txtArguments.Text; }
            set { txtArguments.Text = value; }
        }
    }
}
