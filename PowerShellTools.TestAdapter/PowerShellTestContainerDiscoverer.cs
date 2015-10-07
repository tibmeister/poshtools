using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using PowerShellTools.Common.Logging;
using XmlTestAdapter;
using XmlTestAdapter.EventWatchers;
using XmlTestAdapter.EventWatchers.EventArgs;

namespace PowerShellTools.TestAdapter
{
    [Export(typeof (ITestContainerDiscoverer))]
    public class PowerShellTestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly List<ITestContainer> _cachedContainers;

        private readonly IServiceProvider _serviceProvider;
        private bool _initialContainerSearch;
        private ISolutionEventsListener _solutionListener;
        private ITestFileAddRemoveListener _testFilesAddRemoveListener;
        private ITestFilesUpdateWatcher _testFilesUpdateWatcher;

        private static readonly ILog Log = LogManager.GetLogger(typeof(PowerShellTestContainerDiscoverer));

        [ImportingConstructor]
        public PowerShellTestContainerDiscoverer(
            [Import(typeof (SVsServiceProvider))] IServiceProvider serviceProvider,
            ISolutionEventsListener solutionListener,
            ITestFilesUpdateWatcher testFilesUpdateWatcher,
            ITestFileAddRemoveListener testFilesAddRemoveListener)
        {
            _initialContainerSearch = true;
            _cachedContainers = new List<ITestContainer>();
            _serviceProvider = serviceProvider;
            _solutionListener = solutionListener;
            _testFilesUpdateWatcher = testFilesUpdateWatcher;
            _testFilesAddRemoveListener = testFilesAddRemoveListener;

            _testFilesAddRemoveListener.TestFileChanged += OnProjectItemChanged;
            _testFilesAddRemoveListener.StartListeningForTestFileChanges();

            _solutionListener.SolutionUnloaded += SolutionListenerOnSolutionUnloaded;
            _solutionListener.SolutionProjectChanged += OnSolutionProjectChanged;
            _solutionListener.StartListeningForChanges();

            _testFilesUpdateWatcher.FileChangedEvent += OnProjectItemChanged;
        }

        protected string FileExtension
        {
            get { return ".tests.ps1"; }
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
            Log.Debug("PowerShellTestContainerDiscoverer:OnTestContainersChanged");
            if (TestContainersUpdated != null && !_initialContainerSearch)
            {
                Log.Debug("PowerShellTestContainerDiscoverer:Triggering on TestContainersUpdated");
                TestContainersUpdated(this, EventArgs.Empty);
            }
        }

        private void SolutionListenerOnSolutionUnloaded(object sender, EventArgs eventArgs)
        {
            _initialContainerSearch = true;
        }

        private void OnSolutionProjectChanged(object sender, SolutionEventsListenerEventArgs e)
        {
            Log.Debug("PowerShellTestContainerDiscoverer:OnSolutionProjectChanged");
            if (e != null)
            {
                IEnumerable<string> files = FindPowerShellTestFiles(e.Project);
                if (e.ChangedReason == SolutionChangedReason.Load)
                {
                    Log.Debug("PowerShellTestContainerDiscoverer:OnTestContainersChanged - Change reason is load");
                    UpdateFileWatcher(files, true);
                }
                else if (e.ChangedReason == SolutionChangedReason.Unload)
                {
                    Log.Debug("PowerShellTestContainerDiscoverer:OnTestContainersChanged - Change reason is unload");
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
                    Log.Debug("PowerShellTestContainerDiscoverer:UpdateFileWatcher - AddWatch:" + file);
                    _testFilesUpdateWatcher.AddWatch(file);
                    AddTestContainerIfTestFile(file);
                }
                else
                {
                    Log.Debug("PowerShellTestContainerDiscoverer:UpdateFileWatcher - RemoveWatch:" + file);
                    _testFilesUpdateWatcher.RemoveWatch(file);
                    RemoveTestContainer(file);
                }
            }
        }


        private void OnProjectItemChanged(object sender, TestFileChangedEventArgs e)
        {
            Log.Debug("PowerShellTestContainerDiscoverer:OnProjectItemChanged");
            if (e != null)
            {
                // Don't do anything for files we are sure can't be test files
                if (!IsPowerShellTestFile(e.File)) return;

                Log.Debug("PowerShellTestContainerDiscoverer:OnProjectItemChanged - IsPs1File");

                switch (e.ChangedReason)
                {
                    case TestFileChangedReason.Added:
                        Log.Debug("PowerShellTestContainerDiscoverer:OnProjectItemChanged - Added");
                        _testFilesUpdateWatcher.AddWatch(e.File);
                        AddTestContainerIfTestFile(e.File);

                        break;
                    case TestFileChangedReason.Removed:
                        Log.Debug("PowerShellTestContainerDiscoverer:OnProjectItemChanged - Removed");
                        _testFilesUpdateWatcher.RemoveWatch(e.File);
                        RemoveTestContainer(e.File);

                        break;
                    case TestFileChangedReason.Changed:
                        Log.Debug("PowerShellTestContainerDiscoverer:OnProjectItemChanged - Changed");
                        AddTestContainerIfTestFile(e.File);
                        break;
                }

                OnTestContainersChanged();
            }
        }

        private void AddTestContainerIfTestFile(string file)
        {
            bool isTestFile = IsPowerShellTestFile(file);
            RemoveTestContainer(file); // Remove if there is an existing container

            // If this is a test file
            if (isTestFile)
            {
                Log.Debug("PowerShellTestContainerDiscoverer:AddTestContainerIfTestFile - Is a test file. Adding to cached containers.");
                var container = new PowerShellTestContainer(this, file, ExecutorUri);
                _cachedContainers.Add(container);
            }
        }

        private void RemoveTestContainer(string file)
        {
            int index = _cachedContainers.FindIndex(x => x.Source.Equals(file, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                Log.DebugFormat("PowerShellTestContainerDiscoverer:RemoveTestContainer - Removing [{0}] from cached containers.",file);
                _cachedContainers.RemoveAt(index);
            }
        }

        private IEnumerable<ITestContainer> GetTestContainers()
        {
            if (_initialContainerSearch)
            {
                _cachedContainers.Clear();
                IEnumerable<string> testFiles = FindPowerShellTestFiles();
                UpdateFileWatcher(testFiles, true);
                _initialContainerSearch = false;
            }

            return _cachedContainers;
        }

        private IEnumerable<string> FindPowerShellTestFiles()
        {
            var solution = (IVsSolution) _serviceProvider.GetService(typeof (SVsSolution));
            IEnumerable<IVsProject> loadedProjects =
                solution.EnumerateLoadedProjects(__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION).OfType<IVsProject>();

            return loadedProjects.SelectMany(FindPowerShellTestFiles).ToList();
        }

        private IEnumerable<string> FindPowerShellTestFiles(IVsProject project)
        {
            Log.Debug("PowerShellTestContainerDiscoverer:OnTestContainersChanged - FindPs1Files");
            return from item in VsSolutionHelper.GetProjectItems(project)
                where IsPowerShellTestFile(item)
                select item;
        }

        private bool IsPowerShellTestFile(string path)
        {
            try
            {
                Log.Debug("PowerShellTestContainerDiscoverer:IsTestFile - " + path);
                return path.EndsWith(".tests.ps1", StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException e)
            {
                Log.Debug("IO error when detecting a test file during Test Container Discovery" + e.Message);
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