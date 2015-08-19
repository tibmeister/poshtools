using PowerShellTools.Common.Debugging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerShellTools.DebugEngine.PromptUI
{
    public class ReadHostPromptDialogViewModel : INotifyPropertyChanged
    {
        private string _parameterValue;
        private string _parameterMessage;
        private string _parameterName;
        private string _title;

        public ReadHostPromptDialogViewModel(string paramMessage, string parameterName)
        {
            _parameterMessage = string.IsNullOrEmpty(paramMessage) ? DebugEngineConstants.ReadHostDialogTitle : paramMessage;
            _parameterName = parameterName;
            _parameterValue = string.Empty;
            _title = string.IsNullOrEmpty(parameterName) ? DebugEngineConstants.ReadHostDialogTitle : parameterName;
        }

        /// <summary>
        /// Message of parameter
        /// </summary>
        public string ParameterMessage
        {
            get
            {
                return _parameterMessage;
            }
        }

        /// <summary>
        /// Name of parameter
        /// </summary>
        public string ParameterName
        {
            get
            {
                return _parameterName;
            }
        }

        /// <summary>
        /// Title of the dialog window
        /// </summary>
        public string Title
        {
            get
            {
                return _title;
            }
        }

        /// <summary>
        /// Parameter value
        /// </summary>
        public string ParameterValue
        {
            get
            {
                return _parameterValue;
            }
            set
            {
                _parameterValue = value;
            }
        }

        /// <summary>
        /// Raised when the value of a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        private void OnPropertyChanged(string propertyName)
        {
            var evt = PropertyChanged;
            if (evt != null)
            {
                evt(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}