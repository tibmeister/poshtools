using System;
using Microsoft.VisualStudio.Imaging.Interop;

namespace PowerShellTools.Project.Images
{
    public static class PowerShellMonikers
    {
        private static readonly Guid ManifestGuid = new Guid("072A9D46-0C24-4CFA-990A-25D7E12271F5");

        private const int ProjectIcon = 0;
        private const int ScriptIcon = 1;
        private const int DataIcon = 2;
        private const int ModuleIcon = 3;

        public static ImageMoniker ProjectIconImageMoniker
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ProjectIcon };
            }
        }

        public static ImageMoniker ScriptIconImageMoniker
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ScriptIcon };
            }
        }

        public static ImageMoniker DataIconImageMoniker
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = DataIcon };
            }
        }

        public static ImageMoniker ModuleIconImageMoniker
        {
            get
            {
                return new ImageMoniker { Guid = ManifestGuid, Id = ModuleIcon };
            }
        }
    }
}
