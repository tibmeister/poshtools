using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudioTools.Project;
using PowerShellTools.Project.Images;

namespace PowerShellTools.Project
{
    internal class PowerShellFileNode : CommonFileNode
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PowerShellFileNode"/> class.
        /// </summary>
        /// <param name="root">The project node.</param>
        /// <param name="e">The project element node.</param>
        internal PowerShellFileNode(CommonProjectNode root, ProjectElement e)
            : base(root, e)
        {
        }
        #endregion

        protected override NodeProperties CreatePropertiesObject()
        {
            return new PowerShellFileNodeProperties(this);
        }

#if DEV14_OR_LATER
        protected override bool SupportsIconMonikers { get { return true; } }

        protected override ImageMoniker CodeFileIconMoniker
        {
            get
            {
                if (FileName.EndsWith(PowerShellConstants.PSM1File))
                {
                    return PowerShellMonikers.ModuleIconImageMoniker;
                }

                if (FileName.EndsWith(PowerShellConstants.PSD1File))
                {
                    return PowerShellMonikers.DataIconImageMoniker;
                }

                return PowerShellMonikers.ScriptIconImageMoniker;
            }
        }
#else
        public override int ImageIndex
        {
            get
            {
                if (ItemNode.IsExcluded)
                {
                    return (int)ProjectNode.ImageName.ExcludedFile;
                }
                else if (!File.Exists(Url))
                {
                    return (int)ProjectNode.ImageName.MissingFile;
                }
                else if (IsFormSubType)
                {
                    return (int)ProjectNode.ImageName.WindowsForm;
                }
                else if (this.ProjectMgr.IsCodeFile(FileName))
                {
                    ImageListIndex index = ImageListIndex.Script;

                    if (FileName.EndsWith(PowerShellConstants.PSM1File))
                    {
                        index = ImageListIndex.Module;
                    }
                    else if (FileName.EndsWith(PowerShellConstants.PSD1File))
                    {
                        index = ImageListIndex.DataFile;
                    }
                    else if (FileName.EndsWith(PowerShellConstants.Test))
                    {
                        index = ImageListIndex.Test;
                    }

                    return (int)index;
                }

                return base.ImageIndex;
            }
        }
#endif

        internal override int QueryStatusOnNode(Guid guidCmdGroup, uint cmd, IntPtr pCmdText, ref QueryStatusResult result)
        {
            if (guidCmdGroup == VsMenus.guidStandardCommandSet97 && IsFormSubType)
            {
                switch ((VSConstants.VSStd97CmdID)cmd)
                {
                    case VSConstants.VSStd97CmdID.ViewForm:
                        result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                        return VSConstants.S_OK;
                }
            }

            return base.QueryStatusOnNode(guidCmdGroup, cmd, pCmdText, ref result);
        }
    }
}
