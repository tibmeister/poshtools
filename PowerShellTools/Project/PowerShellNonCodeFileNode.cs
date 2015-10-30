using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudioTools.Project;

namespace PowerShellTools.Project
{
    class PowerShellNonCodeFileNode : CommonNonCodeFileNode
    {
        private object _designerContext;

        public PowerShellNonCodeFileNode(CommonProjectNode root, ProjectElement e)
            : base(root, e)
        {
        }

        protected internal object DesignerContext
        {
            get
            {
                if (_designerContext == null)
                {
                    _designerContext = XamlDesignerSupport.CreateDesignerContext();
                    //Set the EventBindingProvider for this XAML file so the designer will call it
                    //when event handlers need to be generated
                    var dirName = Path.GetDirectoryName(Url);
                    var fileName = Path.GetFileNameWithoutExtension(Url);
                    var filenameWithoutExt = Path.Combine(dirName, fileName);

                    // look for ps1
                    var child = ProjectMgr.FindNodeByFullPath(filenameWithoutExt + PowerShellConstants.PS1File);
                    if (child != null)
                    {
                        XamlDesignerSupport.InitializeEventBindingProvider(_designerContext, child as PowerShellFileNode);
                    }
                }
                return _designerContext;
            }
        }

        public override int QueryService(ref Guid guidService, out object result)
        {
            if (XamlDesignerSupport.DesignerContextType != null &&
                guidService == XamlDesignerSupport.DesignerContextType.GUID &&
                Path.GetExtension(Url).Equals(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                // Create a DesignerContext for the XAML designer for this file
                result = DesignerContext;
                return VSConstants.S_OK;
            }

            return base.QueryService(ref guidService, out result);
        }
    }
    }
