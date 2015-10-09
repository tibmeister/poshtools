using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ReferenceManager.Contracts;
using Microsoft.VisualStudio.ReferenceManager.Providers;
using Microsoft.VisualStudio.ReferenceManager.Providers.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace PowerShellTools.Project
{
    [Export(typeof(IReferenceProvider))]
    [ExportMetadata("Name", "ModuleReferenceProvider")]
    [ExportMetadata("Guid", Common.Constants.ModuleReferenceProvider_string)]
    internal class ModuleReferenceProvider : StandardReferenceProvider
    {
        private ModuleReferenceProviderContext _providerContext;
        private bool alreadyPopulated;

        public ModuleReferenceProvider() : base(Common.Constants.ModuleReferenceProvider_guid)
        {
        }

        public ModuleReferenceProvider(bool showRecentTab) : base(Common.Constants.ModuleReferenceProvider_guid, showRecentTab)
        {
        }

        public ModuleReferenceProvider(bool showRecentTab, bool alwaysShowRecentTab) : base(Common.Constants.ModuleReferenceProvider_guid, showRecentTab, alwaysShowRecentTab)
        {
        }

        protected override IEnumerable<IVsReferenceProviderContext> GetChangeContextWithReferences(IEnumerable<Microsoft.VisualStudio.ReferenceManager.Providers.StandardReferenceItem> itemsToChange, bool add)
        {
            yield return new ModuleReferenceProviderContext();
        }

        protected override IEnumerable<StandardReferenceItem> LoadRecentReferencesFromCacheFile()
        {
            return new StandardReferenceItem[] {};
        }

        protected override void SaveRecentReferencesToCacheFile(IEnumerable<StandardReferenceItem> assemblies)
        {
            
        }

        protected override void BeforeShutdown()
        {
         
        }

        public override void SetContext(IVsReferenceProviderContext context)
        {
            base.SetContext(context);
            if (_providerContext == null)
            {
                SetupProvider(context);
            }
            else
            {
                var context2 = context as ModuleReferenceProviderContext;
                if (context2 != null)
                {
                    MarkReferencedItems(Subcategories["Modules"].Items,context2.References.Cast<IVsReference>());
                }
            }
        }

        public override void ProviderSelected(bool selected)
        {
            base.ProviderSelected(selected);
            if (selected && !this.alreadyPopulated)
            {
                Dispatcher.BeginInvoke((MethodInvoker) delegate {
                    if (!alreadyPopulated)
                    {
                        LoadModules();
                        var node = base.Subcategories["Modules"];
                        if (node.Items.Count == 0)
                        {
                            node.NoItemsMessage = null;
                        }
                        IsContentLoaded = alreadyPopulated;
                    }
                });
            }
        }

        private void LoadModules()
        {
            var items = base.Subcategories["Modules"].Items;
            using (var ps = System.Management.Automation.PowerShell.Create())
            {
                ps.AddCommand("Get-Module").AddParameter("ListAvailable");
                foreach (var module in ps.Invoke<PSModuleInfo>())
                {
                    items.Add(new ModuleReferenceItem(module.Name, module.Path));
                }
            }
        }

        private void SetupProvider(IVsReferenceProviderContext context)
        {
            GridView defaultGridView = (GridView)base.Resources["ProjectsGridView"];
            ColumnInfo[] columnInfos = new ColumnInfo[] { new ColumnInfo("AlreadyReferenced", null), new ColumnInfo("DisplayName", null), new ColumnInfo("FullPath", null) };
            base.Initialize("Modules", context, defaultGridView, columnInfos, "DisplayName");
            this.alreadyPopulated = false;
            _providerContext = context as ModuleReferenceProviderContext;
            if (_providerContext == null)
            {
                Marshal.ThrowExceptionForHR(-2147024809);
            }
            base.AddSubCategory("Modules", "Modules");
            base.SetFooterText(false, string.Empty);
        }


    }

    [Export(typeof(IVsReferenceProviderContext))]
    [Export(typeof(IVsAssemblyReferenceProviderContext))]
    [Export("ModuleReferenceProviderContext", typeof(IVsReferenceProviderContext))]
    [Export(Common.Constants.ModuleReferenceProvider_string, typeof(IVsReferenceProviderContext))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [ExportMetadata("Name", "ModuleReferenceProviderContext")]
    [ExportMetadata("Guid", Common.Constants.ModuleReferenceProvider_string)]
    public class ModuleReferenceProviderContext : StandardReferenceProviderContext<IVsReference, ModuleReferenceItem>, IVsAssemblyReferenceProviderContext
    {
        public ModuleReferenceProviderContext() : base(Common.Constants.ModuleReferenceProvider_guid)
        {
        }

        public string AssemblySearchPaths { get; set; }
        public string TargetFrameworkMoniker { get; set; }
        public uint Tabs { get; set; }
        public bool SupportsRetargeting { get; set; }
        public bool IsImplicitlyReferenced { get; set; }
        public string RetargetingMessage { get; set; }
    }

    [Serializable]
    public class ModuleReferenceItem : StandardReferenceItem
    {
        public ModuleReferenceItem()
        {
            
        }
        public ModuleReferenceItem(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
            DisplayName = name;
        }

        public override string ToolTipText
        {
            get { return DisplayName; }
        }

        public override bool Equals(object obj)
        {
            var moduleReference = obj as ModuleReferenceItem;
            if (moduleReference == null) return false;

            return string.Equals(moduleReference.FullPath, FullPath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
