using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using log4net;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudioTools;
using Microsoft.VisualStudioTools.Navigation;
using PowerShellTools.Classification;
using PowerShellTools.Commands;
using PowerShellTools.Common.ServiceManagement.DebuggingContract;
using PowerShellTools.Common.ServiceManagement.IntelliSenseContract;
using PowerShellTools.Contracts;
using PowerShellTools.DebugEngine;
using PowerShellTools.Diagnostics;
using PowerShellTools.Intellisense;
using PowerShellTools.LanguageService;
using PowerShellTools.Project.PropertyPages;
using PowerShellTools.Service;
using PowerShellTools.ServiceManagement;
using Engine = PowerShellTools.DebugEngine.Engine;
using MessageBox = System.Windows.MessageBox;
using Threading = System.Threading.Tasks;

namespace PowerShellTools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    //[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    [ProvideLanguageService(typeof(PowerShellLanguageInfo),
                            PowerShellConstants.LanguageName,
                            101,
                            ShowSmartIndent = true,
                            ShowDropDownOptions = true,
                            EnableCommenting = true)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideKeyBindingTable(GuidList.guidCustomEditorEditorFactoryString, 102)]
    [Guid(GuidList.PowerShellToolsPackageGuid)]
    [ProvideObject(typeof(InformationPropertyPage))]
    [ProvideObject(typeof(ComponentsPropertyPage))]
    [ProvideObject(typeof(ExportsPropertyPage))]
    [ProvideObject(typeof(RequirementsPropertyPage))]
    [ProvideObject(typeof(DebugPropertyPage))]
    [Microsoft.VisualStudio.Shell.ProvideDebugEngine("{43ACAB74-8226-4920-B489-BFCF05372437}", "PowerShell",
        PortSupplier = "{708C1ECA-FF48-11D2-904F-00C04FA302A1}",
        ProgramProvider = "{08F3B557-C153-4F6C-8745-227439E55E79}", Attach = true,
        CLSID = "{C7F9F131-53AB-4FD0-8517-E54D124EA392}")]
    [Clsid(Clsid = "{C7F9F131-53AB-4FD0-8517-E54D124EA392}",
           Assembly = "PowerGuiVsx.Core.DebugEngine",
        Class = "PowerGuiVsx.Core.DebugEngine.Engine")]
    [Clsid(Clsid = "{08F3B557-C153-4F6C-8745-227439E55E79}",
           Assembly = "PowerGuiVsx.Core.DebugEngine",
        Class = "PowerGuiVsx.Core.DebugEngine.ScriptProgramProvider")]
    [Microsoft.VisualStudioTools.ProvideDebugEngine("PowerShell",
                                                    typeof(ScriptProgramProvider),
                                                    typeof(Engine),
        "{43ACAB74-8226-4920-B489-BFCF05372437}")]
    [ProvideIncompatibleEngineInfo("{92EF0900-2251-11D2-B72E-0000F87572EF}")]
    [ProvideIncompatibleEngineInfo("{F200A7E7-DEA5-11D0-B854-00A0244A1DE2}")]
    [ProvideOptionPage(typeof(GeneralDialogPage), PowerShellConstants.LanguageDisplayName, "General", 101, 106, true)]
    [ProvideOptionPage(typeof(DiagnosticsDialogPage), PowerShellConstants.LanguageDisplayName, "Diagnostics", 101, 106, true)]
    [ProvideDiffSupportedContentType(".ps1;.psm1;.psd1", ";")]
    [ProvideLanguageExtension(typeof(PowerShellLanguageInfo), ".ps1")]
    [ProvideLanguageExtension(typeof(PowerShellLanguageInfo), ".psm1")]
    [ProvideLanguageExtension(typeof(PowerShellLanguageInfo), ".psd1")]
    [ProvideLanguageCodeExpansion(typeof(PowerShellLanguageInfo),
                                  "PowerShell",        // Name of language used as registry key
                                  0,                   // Resource ID of localized name of language service
         "PowerShell",        // Name of Language attribute in snippet template
         @"%TestDocs%\Code Snippets\PowerShel\SnippetsIndex.xml",  // Path to snippets index
         SearchPaths = @"%TestDocs%\Code Snippets\PowerShell\")]    // Path to snippets
    [ProvideService(typeof(IPowerShellService))]

    public sealed class PowerShellToolsPackage : CommonPackage
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PowerShellToolsPackage));
        private Lazy<PowerShellService> _powershellService;
        private static ScriptDebugger _debugger;
        private ITextBufferFactoryService _textBufferFactoryService;
        private static Dictionary<ICommand, MenuCommand> _commands;
        private static GotoDefinitionCommand _gotoDefinitionCommand;
        private VisualStudioEvents VisualStudioEvents;
        private IContentType _contentType;
        private IntelliSenseEventsHandlerProxy _intelliSenseServiceContext;

        public static EventWaitHandle DebuggerReadyEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

        public static bool PowerShellHostInitialized = false;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public PowerShellToolsPackage()
        {
            Log.Info(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this));
            Instance = this;
            _commands = new Dictionary<ICommand, MenuCommand>();
            DependencyValidator = new DependencyValidator();
        }

        /// <summary>
        /// Returns the current package instance.
        /// </summary>
        public static PowerShellToolsPackage Instance { get; private set; }

        public static IPowershellDebuggingService DebuggingService
        {
            get
            {
                return ConnectionManager.Instance.PowershellDebuggingService;
            }
        }

        public IntelliSenseEventsHandlerProxy IntelliSenseServiceContext
        {
            get
            {
                return _intelliSenseServiceContext;
            }
        }

        public new object GetService(Type type)
        {
            return base.GetService(type);
        }

        public override Type GetLibraryManagerType()
        {
            return null;
        }

        public override bool IsRecognizedFile(string filename)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the PowerShell host for the package.
        /// </summary>
        internal static ScriptDebugger Debugger
        {
            get
            {
                return _debugger;
            }
        }

        /// <summary>
        /// Indicate if override the execution policy
        /// </summary>
        internal static bool OverrideExecutionPolicyConfiguration { get; private set; }

        internal static IPowershellIntelliSenseService IntelliSenseService
        {
            get
            {
                return ConnectionManager.Instance.PowershellIntelliSenseSerivce;
            }
        }

        internal DependencyValidator DependencyValidator { get; set; }

        internal override LibraryManager CreateLibraryManager(CommonPackage package)
        {
            throw new NotImplementedException();
        }

        internal IContentType ContentType
        {
            get
            {
                if (_contentType == null)
                {
                    _contentType = ComponentModel.GetService<IContentTypeRegistryService>().GetContentType(PowerShellConstants.LanguageName);
                }
                return _contentType;
            }
        }

        internal T GetDialogPage<T>() where T : DialogPage
        {
            return (T)GetDialogPage(typeof(T));
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                if (!DependencyValidator.Validate())
                {
                    return;
                }

                base.Initialize();

                InitializeInternal();

                _powershellService = new Lazy<PowerShellService>(() => { return new PowerShellService(); });

                RegisterServices();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    Resources.PowerShellToolsInitializeFailed + ex,
                    Resources.MessageBoxErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void InitializeInternal()
        {
            _intelliSenseServiceContext = new IntelliSenseEventsHandlerProxy();

            var page = (DiagnosticsDialogPage)GetDialogPage(typeof(DiagnosticsDialogPage));

            if (page.EnableDiagnosticLogging)
            {
                DiagnosticConfiguration.EnableDiagnostics();
            }

            Log.Info(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this));

            var langService = new PowerShellLanguageInfo(this);
            ((IServiceContainer)this).AddService(langService.GetType(), langService, true);

            var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            _textBufferFactoryService = componentModel.GetService<ITextBufferFactoryService>();
            EditorImports.ClassificationTypeRegistryService = componentModel.GetService<IClassificationTypeRegistryService>();
            EditorImports.ClassificationFormatMap = componentModel.GetService<IClassificationFormatMapService>();
            VisualStudioEvents = componentModel.GetService<VisualStudioEvents>();

            if (VisualStudioEvents != null)
            {
                VisualStudioEvents.SettingsChanged += VisualStudioEvents_SettingsChanged;
            }

            if (_textBufferFactoryService != null)
            {
                _textBufferFactoryService.TextBufferCreated += TextBufferFactoryService_TextBufferCreated;
            }

            _gotoDefinitionCommand = new GotoDefinitionCommand();

            RefreshCommands(new ExecuteSelectionCommand(this.DependencyValidator),
                            new ExecuteFromEditorContextMenuCommand(this.DependencyValidator),
                            new ExecuteFromSolutionExplorerContextMenuCommand(this.DependencyValidator),
                            _gotoDefinitionCommand,
                            new PrettyPrintCommand(),
                            new OpenDebugReplCommand());

            try
            {
                Threading.Task.Run(
                    () =>
                    {
                        InitializePowerShellHost();
                    }
                );
            }
            catch (AggregateException ae)
            {
                MessageBox.Show(
                    Resources.PowerShellHostInitializeFailed,
                    Resources.MessageBoxErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                throw ae.Flatten();
            }
        }

        private void RefreshCommands(params ICommand[] commands)
        {
            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                foreach (var command in commands)
                {
                    var menuCommand = new OleMenuCommand(command.Execute, command.CommandId);
                    menuCommand.BeforeQueryStatus += command.QueryStatus;
                    mcs.AddCommand(menuCommand);
                    _commands[command] = menuCommand;
                }
            }
        }

        /// <summary>
        /// Register Services
        /// </summary>
        private void RegisterServices()
        {
            Debug.Assert(this is IServiceContainer, "The package is expected to be an IServiceContainer.");

            var serviceContainer = (IServiceContainer)this;
            serviceContainer.AddService(typeof(IPowerShellService), (c, t) => _powershellService.Value, true);
        }

        private void VisualStudioEvents_SettingsChanged(object sender, DialogPage e)
        {
            if (e is DiagnosticsDialogPage)
            {
                var page = (DiagnosticsDialogPage)e;
                if (page.EnableDiagnosticLogging)
                {
                    DiagnosticConfiguration.EnableDiagnostics();
                }
                else
                {
                    DiagnosticConfiguration.DisableDiagnostics();
                }
            }
        }

        private static void TextBufferFactoryService_TextBufferCreated(object sender, TextBufferCreatedEventArgs e)
        {
            ITextBuffer buffer = e.TextBuffer;

            buffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;

            EnsureBufferHasTokenizer(e.TextBuffer.ContentType, buffer);
        }

        private static void TextBuffer_ContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            var buffer = sender as ITextBuffer;

            Debug.Assert(buffer != null, "buffer is null");

            EnsureBufferHasTokenizer(e.AfterContentType, buffer);
        }

        private static void EnsureBufferHasTokenizer(IContentType contentType, ITextBuffer buffer)
        {
            if (contentType.IsOfType(PowerShellConstants.LanguageName) && !buffer.Properties.ContainsProperty(BufferProperties.PowerShellTokenizer))
            {
                IPowerShellTokenizationService psts = new PowerShellTokenizationService(buffer);

                _gotoDefinitionCommand.AddTextBuffer(buffer);
                buffer.PostChanged += (o, args) => psts.StartTokenization();

                buffer.Properties.AddProperty(BufferProperties.PowerShellTokenizer, psts);
            }
        }

        /// <summary>
        /// Initialize the PowerShell host.
        /// </summary>
        private void InitializePowerShellHost()
        {
            var page = (GeneralDialogPage)GetDialogPage(typeof(GeneralDialogPage));
            OverrideExecutionPolicyConfiguration = page.OverrideExecutionPolicyConfiguration;

            Log.Info("InitializePowerShellHost");

            _debugger = new ScriptDebugger(page.OverrideExecutionPolicyConfiguration);

            // Warm up intellisense service due to the reason that first intellisense request sometime slow than usual
            IntelliSenseService.GetDummyCompletionList();

            DebuggerReadyEvent.Set();

            PowerShellHostInitialized = true;
        }
    }
}
