using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell;

namespace PowerShellTools
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    class VisualStudioEvents
    {
        public event EventHandler<DialogPage> SettingsChanged;

        public void OnSettingsChanged(DialogPage dialogPageType)
        {
            if (SettingsChanged != null) SettingsChanged(this, dialogPageType);
        }
    }
}
