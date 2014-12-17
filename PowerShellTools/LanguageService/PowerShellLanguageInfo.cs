using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace PowerShellTools.LanguageService
{
        /// <summary>
        /// Minimal language service.  Implemented directly rather than using the Managed Package
        /// Framework because we don't want to provide colorization services.  Instead we use the
        /// new Visual Studio 2010 APIs to provide these services.  But we still need this to
        /// provide a code window manager so that we can have a navigation bar (actually we don't, this
        /// should be switched over to using our TextViewCreationListener instead).
        /// </summary>
        [Guid("1C4711F1-3766-4F84-9516-43FA4169CC36")]
        internal sealed class PowerShellLanguageInfo : IVsLanguageInfo, IVsLanguageDebugInfo
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly IComponentModel _componentModel;

            public PowerShellLanguageInfo(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
                _componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            }

            public int GetCodeWindowManager(IVsCodeWindow pCodeWin, out IVsCodeWindowManager ppCodeWinMgr)
            {
                var model = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
                var service = model.GetService<IVsEditorAdaptersFactoryService>();

                IVsTextView textView;
                if (ErrorHandler.Succeeded(pCodeWin.GetPrimaryView(out textView)))
                {
                    ppCodeWinMgr = new CodeWindowManager(pCodeWin, service.GetWpfTextView(textView));
                    return VSConstants.S_OK;
                }

                ppCodeWinMgr = null;
                return VSConstants.E_FAIL;
            }

            public int GetFileExtensions(out string pbstrExtensions)
            {
                // This is the same extension the language service was
                // registered as supporting.
                pbstrExtensions = PowerShellConstants.PS1File + ";" + PowerShellConstants.PSD1File + ";" + PowerShellConstants.PSM1File;
                return VSConstants.S_OK;
            }


            public int GetLanguageName(out string bstrName)
            {
                // This is the same name the language service was registered with.
                bstrName = PowerShellConstants.LanguageName;
                return VSConstants.S_OK;
            }

            /// <summary>
            /// GetColorizer is not implemented because we implement colorization using the new managed APIs.
            /// </summary>
            public int GetColorizer(IVsTextLines pBuffer, out IVsColorizer ppColorizer)
            {
                ppColorizer = null;
                return VSConstants.E_FAIL;
            }

            public IServiceProvider ServiceProvider
            {
                get
                {
                    return _serviceProvider;
                }
            }

            #region IVsLanguageDebugInfo Members

            public int GetLanguageID(IVsTextBuffer pBuffer, int iLine, int iCol, out Guid pguidLanguageID)
            {
                pguidLanguageID = Guid.Empty;
                return VSConstants.S_OK;
            }

            public int GetLocationOfName(string pszName, out string pbstrMkDoc, TextSpan[] pspanLocation)
            {
                pbstrMkDoc = null;
                return VSConstants.E_FAIL;
            }

            public int GetNameOfLocation(IVsTextBuffer pBuffer, int iLine, int iCol, out string pbstrName, out int piLineOffset)
            {
                var model = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
                var service = model.GetService<IVsEditorAdaptersFactoryService>();
                var buffer = service.GetDataBuffer(pBuffer);

                pbstrName = "";
                piLineOffset = iCol;
                return VSConstants.E_FAIL;
            }

            public int GetProximityExpressions(IVsTextBuffer pBuffer, int iLine, int iCol, int cLines, out IVsEnumBSTR ppEnum)
            {
                ppEnum = null;
                return VSConstants.E_FAIL;
            }

            public int IsMappedLocation(IVsTextBuffer pBuffer, int iLine, int iCol)
            {
                return VSConstants.E_FAIL;
            }

            public int ResolveName(string pszName, uint dwFlags, out IVsEnumDebugName ppNames)
            {
                /*if((((RESOLVENAMEFLAGS)dwFlags) & RESOLVENAMEFLAGS.RNF_BREAKPOINT) != 0) {
                    // TODO: This should go through the project/analysis and see if we can
                    // resolve the names...
                }*/
                ppNames = null;
                return VSConstants.E_FAIL;
            }

            public int ValidateBreakpointLocation(IVsTextBuffer pBuffer, int iLine, int iCol, TextSpan[] pCodeSpan)
            {
                // per the docs, even if we don't indend to validate, we need to set the span info:
                // http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.textmanager.interop.ivslanguagedebuginfo.validatebreakpointlocation.aspx
                // 
                // Caution
                // Even if you do not intend to support the ValidateBreakpointLocation method but your 
                // language does support breakpoints, you must implement this method and return a span 
                // that contains the specified line and column; otherwise, breakpoints cannot be set 
                // anywhere except line 1. You can return E_NOTIMPL to indicate that you do not otherwise 
                // support this method but the span must always be set. The example shows how this can be done.

                // http://pytools.codeplex.com/workitem/787
                // We were previously returning S_OK here indicating to VS that we have in fact validated
                // the breakpoint.  Validating breakpoints actually interacts and effectively disables
                // the "Highlight entire source line for breakpoints and current statement" option as instead
                // VS highlights the validated region.  So we return E_NOTIMPL here to indicate that we have 
                // not validated the breakpoint, and then VS will happily respect the option when we're in 
                // design mode.
                pCodeSpan[0].iStartLine = iLine;
                pCodeSpan[0].iEndLine = iLine;
                return VSConstants.E_NOTIMPL;
            }



            #endregion
        }
}
