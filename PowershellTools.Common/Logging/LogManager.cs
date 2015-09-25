using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using log4net.Config;

namespace PowerShellTools.Common.Logging
{
    public class LogManager
    {
        private static bool _initialized;
        private const string ResourceName = "PowerShellTools.Common.Logging.logging.xml";
        private static readonly string PoshToolsAppPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"PowerShell Tools for Visual Studio");
        private static readonly string LogConfigPath = Path.Combine(PoshToolsAppPath, "logging.xml");

        public static ILog GetLogger(Type type)
        {
            Initialize();

            return new Log(log4net.LogManager.GetLogger(type));
        }

        private static void Initialize()
        {
            if (_initialized) return;

            if (!File.Exists(LogConfigPath))
            {
                Directory.CreateDirectory(PoshToolsAppPath);
                var assembly = Assembly.GetExecutingAssembly();

                using (var stream = assembly.GetManifestResourceStream(ResourceName))
                using (var reader = new StreamReader(stream))
                {
                    var result = reader.ReadToEnd();
                    using (var writer = new StreamWriter(LogConfigPath))
                    {
                        writer.Write(result);
                    }
                }
            }

            XmlConfigurator.ConfigureAndWatch(new FileInfo(LogConfigPath));
            
            _initialized = true;
        }

        public static void SetLoggingLevel(string levelString)
        {
            Initialize();
            if (!File.Exists(LogConfigPath)) return;

            var document = XDocument.Load(LogConfigPath);
            if (document.Root == null) return;
            var root = document.Root.Element("root");
            if (root == null) return;
            var level = root.Element("level");
            if (level == null) return;
            var levelValue = level.Attribute("value");
            if (levelValue == null) return;
            levelValue.SetValue(levelString);

            document.Save(LogConfigPath);
        }

    }
}
