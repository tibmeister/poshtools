using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using XmlTestAdapter;
using XmlTestAdapter.EventWatchers;
using XmlTestAdapter.EventWatchers.EventArgs;

namespace PowerShellTools.TestAdapter
{
    [Export(typeof (ITestContainerDiscoverer))]
    public class PowerShellTestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly List<ITestContainer> _cachedContainers;

        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _initialContainerSearch;
        private ISolutionEventsListener _solutionListener;
        private ITestFileAddRemoveListener _testFilesAddRemoveListener;
        private ITestFilesUpdateWatcher _testFilesUpdateWatcher;

        [ImportingConstructor]
        public PowerShellTestContainerDiscoverer(
            [Import(typeof (SVsServiceProvider))] IServiceProvider serviceProvider,
            ILogger logger,
            ISolutionEventsListener solutionListener,
            ITestFilesUpdateWatcher testFilesUpdateWatcher,
            ITestFileAddRemoveListener testFilesAddRemoveListener)
        {
            _initialContainerSearch = true;
            _cachedContainers = new List<ITestContainer>();
            _serviceProvider = serviceProvider;
            _logger = logger;
            _solutionListener = solutionListener;
            _testFilesUpdateWatcher = testFilesUpdateWatcher;
            _testFilesAddRemoveListener = testFilesAddRemoveListener;

            logger.Log(MessageLevel.Diagnostic, "PowerShellTestContainerDiscoverer Constructor Entering");

            _testFilesAddRemoveListener.TestFileChanged += OnProjectItemChanged;
            _testFilesAddRemoveListener.StartListeningForTestFileChanges();

            _solutionListener.SolutionUnloaded += SolutionListenerOnSolutionUnloaded;
            _solutionListener.SolutionProjectChanged += OnSolutionProjectChanged;
            _solutionListener.StartListeningForChanges();

            _testFilesUpdateWatcher.FileChangedEvent += OnProjectItemChanged;
        }

        protected string FileExtension
        {
            get { return ".ps1"; }
        }

        public event EventHandler TestContainersUpdated;

        public Uri ExecutorUri
        {
            get { return PowerShellTestExecutor.ExecutorUri; }
        }

        public IEnumerable<ITestContainer> TestContainers
        {
            get { return GetTestContainers(); }
        }

        private void OnTestContainersChanged()
        {
            _logger.Log(MessageLevel.Diagnostic, "PowerShellTestContainerDiscoverer:OnTestContainersChanged");
            if (TestContainersUpdated != null && !_initialContainerSearch)
            {
                _logger.Log(MessageLevel.Diagnostic,
                    "PowerShellTestContainerDiscoverer:Triggering on TestContainersUpdated");
                TestContainersUpdated(this, EventArgs.Empty);
            }
        }

        private void SolutionListenerOnSolutionUnloaded(object sender, EventArgs eventArgs)
        {
            _initialContainerSearch = true;
        }

        private void OnSolutionProjectChanged(object sender, SolutionEventsListenerEventArgs e)
        {
            _logger.Log(MessageLevel.Diagnostic, "PowerShellTestContainerDiscoverer:OnSolutionProjectChanged");
            if (e != null)
            {
                IEnumerable<string> files = FindPs1Files(e.Project);
                if (e.ChangedReason == SolutionChangedReason.Load)
                {
                    _logger.Log(MessageLevel.Diagnostic,
                        "PowerShellTestContainerDiscoverer:OnTestContainersChanged - Change reason is load");
                    UpdateFileWatcher(files, true);
                }
                else if (e.ChangedReason == SolutionChangedReason.Unload)
                {
                    _logger.Log(MessageLevel.Diagnostic,
                        "PowerShellTestContainerDiscoverer:OnTestContainersChanged - Change reason is unload");
                    UpdateFileWatcher(files, false);
                }
            }

            // Do not fire OnTestContainersChanged here.
            // This will cause us to fire this event too early before the UTE is ready to process containers and will result in an exception.
            // The UTE will query all the TestContainerDiscoverers once the solution is loaded.
        }

        private void UpdateFileWatcher(IEnumerable<string> files, bool isAdd)
        {
            foreach (string file in files)
            {
                if (isAdd)
                {
                    _logger.Log(MessageLevel.Diagnostic,
                        "PowerShellTestContainerDiscoverer:UpdateFileWatcher - AddWatch:" + file);
                    _testFilesUpdateWatcher.AddWatch(file);
                    AddTestContainerIfTestFile(file);
                }
                else
                {
                    _logger.Log(MessageLevel.Diagnostic,
                        "PowerShellTestContainerDiscoverer:UpdateFileWatcher - RemoveWatch:" + file);
                    _testFilesUpdateWatcher.RemoveWatch(file);
                    RemoveTestContainer(file);
                }
            }
        }


        private void OnProjectItemChanged(object sender, TestFileChangedEventArgs e)
        {
            _logger.Log(MessageLevel.Diagnostic, "PowerShellTestContainerDiscoverer:OnProjectItemChanged");
            if (e != null)
            {
                // Don't do anything for files we are sure can't be test files
                if (!IsPs1File(e.File)) return;

                _logger.Log(MessageLevel.Diagnostic, "PowerShellTestContainerDiscoverer:OnProjectItemChanged - IsPs1File");

                switch (e.ChangedReason)
                {
                    case TestFileChangedReason.Added:
                        _logger.Log(MessageLevel.Diagnostic,
                            "PowerShellTestContainerDiscoverer:OnProjectItemChanged - Added");
                        _testFilesUpdateWatcher.AddWatch(e.File);
                        AddTestContainerIfTestFile(e.File);

                        break;
                    case TestFileChangedReason.Removed:
                        _logger.Log(MessageLevel.Diagnostic,
                            "PowerShellTestContainerDiscoverer:OnProjectItemChanged - Removed");
                        _testFilesUpdateWatcher.RemoveWatch(e.File);
                        RemoveTestContainer(e.File);

                        break;
                    case TestFileChangedReason.Changed:
                        _logger.Log(MessageLevel.Diagnostic,
                            "PowerShellTestContainerDiscoverer:OnProjectItemChanged - Changed");
                        AddTestContainerIfTestFile(e.File);
                        break;
                }

                OnTestContainersChanged();
            }
        }

        private void AddTestContainerIfTestFile(string file)
        {
            bool isTestFile = IsTestFile(file);
            RemoveTestContainer(file); // Remove if there is an existing container

            // If this is a test file
            if (isTestFile)
            {
                _logger.Log(MessageLevel.Diagnostic,
                    "PowerShellTestContainerDiscoverer:AddTestContainerIfTestFile - Is a test file. Adding to cached containers.");
                var container = new PowerShellTestContainer(this, file, ExecutorUri);
                _cachedContainers.Add(container);
            }
        }

        private void RemoveTestContainer(string file)
        {
            int index = _cachedContainers.FindIndex(x => x.Source.Equals(file, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                _logger.Log(MessageLevel.Diagnostic,
                    String.Format(
                        "PowerShellTestContainerDiscoverer:RemoveTestContainer - Removing [{0}] from cached containers.",
                        file));
                _cachedContainers.RemoveAt(index);
            }
        }

        private IEnumerable<ITestContainer> GetTestContainers()
        {
            if (_initialContainerSearch)
            {
                _cachedContainers.Clear();
                IEnumerable<string> xmlFiles = FindPs1Files();
                UpdateFileWatcher(xmlFiles, true);
                _initialContainerSearch = false;
            }

            return _cachedContainers;
        }

        private IEnumerable<string> FindPs1Files()
        {
            var solution = (IVsSolution) _serviceProvider.GetService(typeof (SVsSolution));
            IEnumerable<IVsProject> loadedProjects =
                solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

            return loadedProjects.SelectMany(FindPs1Files).ToList();
        }

        private IEnumerable<string> FindPs1Files(IVsProject project)
        {
            _logger.Log(MessageLevel.Diagnostic,
                "PowerShellTestContainerDiscoverer:OnTestContainersChanged - FindPs1Files");
            return from item in VsSolutionHelper.GetProjectItems(project)
                where IsTestFile(item)
                select item;
        }

        private static bool IsPs1File(string path)
        {
            return ".ps1".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        }

        private bool IsTestFile(string path)
        {
            try
            {
                _logger.Log(MessageLevel.Diagnostic, "PowerShellTestContainerDiscoverer:IsTestFile - " + path);
                return IsPs1File(path);
            }
            catch (IOException e)
            {
                _logger.Log(MessageLevel.Error,
                    "IO error when detecting a test file during Test Container Discovery" + e.Message);
            }

            return false;
        }


        public void Dispose()
        {
            Dispose(true);
            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_testFilesUpdateWatcher != null)
                {
                    _testFilesUpdateWatcher.FileChangedEvent -= OnProjectItemChanged;
                    ((IDisposable) _testFilesUpdateWatcher).Dispose();
                    _testFilesUpdateWatcher = null;
                }

                if (_testFilesAddRemoveListener != null)
                {
                    _testFilesAddRemoveListener.TestFileChanged -= OnProjectItemChanged;
                    _testFilesAddRemoveListener.StopListeningForTestFileChanges();
                    _testFilesAddRemoveListener = null;
                }

                if (_solutionListener != null)
                {
                    _solutionListener.SolutionProjectChanged -= OnSolutionProjectChanged;
                    _solutionListener.StopListeningForChanges();
                    _solutionListener = null;
                }
            }
        }
    }
}