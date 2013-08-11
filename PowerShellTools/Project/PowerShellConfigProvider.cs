﻿using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudioTools.Project;
using PowerGUIVsx.Project;

namespace PowerShellTools.Project
{
    internal class PowerShellConfigProvider : CommonConfigProvider
    {
        private CommonProjectNode _node;
        private PowerShellToolsPackage _package;

        public PowerShellConfigProvider(PowerShellToolsPackage package, CommonProjectNode manager)
            : base(manager)
        {
            _package = package;
            _node = manager;
        }



        protected override ProjectConfig CreateProjectConfiguration(string configName)
        {
            return new PowerShellProjectConfig(_package, _node, configName);
        }




    }
}
