using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Management.Automation.Language;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudioTools;
using PowerShellTools.Classification;

namespace PowerShellTools.Commands
{
    internal class GotoDefinitionCommand : ICommand
    {
        private readonly List<ITextBuffer> _textBuffers = new List<ITextBuffer>();

        public void AddTextBuffer(ITextBuffer buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            _textBuffers.Add(buffer);
        }

        private int _offset;
        private Ast _ast;
        private string _fileName;


        public CommandID CommandId
        {
            get
            {
                return new CommandID(new Guid(GuidList.CmdSetGuid), (int)GuidList.CmdidGotoDefinition);
            }
        }
        public void Execute(object sender, EventArgs args)
        {
            var commandAst = _ast.Find(m => m.Extent.StartOffset == _offset, true) as CommandAst;

            if (commandAst == null)
            {
                MessageBox.Show("Whoops! Something went wrong finding the definition of that function!", "Command Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var functionDefinitionAst = FindDefinition(commandAst, commandAst.Parent);

            if (functionDefinitionAst == null)
            {
                MessageBox.Show("Unable to locate the definition to that function.", "Command Warning",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte2 != null)
            {
                var buffer = _textBuffers.FirstOrDefault(
                    m =>
                        m.GetFilePath() != null && m.GetFilePath().Equals(_fileName, StringComparison.OrdinalIgnoreCase));
                if (buffer != null)
                {
                    var ts = dte2.ActiveDocument.Selection as ITextSelection;
                    if (ts != null)
                        ts.Select(new SnapshotSpan(buffer.CurrentSnapshot, _offset, 0), false);
                }
            }
        }

        private static FunctionDefinitionAst FindDefinition(CommandAst ast, Ast parentAst)
        {
            if (ast == null) throw new ArgumentNullException("ast");
            if (parentAst == null) return null;

            var definitions = parentAst.FindAll(
                m => m is FunctionDefinitionAst && ((FunctionDefinitionAst) m).Name.Equals(ast.GetCommandName()), false).ToList();

            if (definitions.Any())
            {
                return definitions.Last() as FunctionDefinitionAst;
            }

            return FindDefinition(ast, parentAst.Parent);
        }

        public void QueryStatus(object sender, EventArgs args)
        {
            bool bVisible = false;

            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            if (dte2 != null && dte2.ActiveDocument != null && dte2.ActiveDocument.Language == "PowerShell")
            {
                var fileName = dte2.ActiveDocument.FullName.ToUpper();
                var buffer = _textBuffers.FirstOrDefault(
                    m =>
                        m.GetFilePath() != null && m.GetFilePath().Equals(fileName, StringComparison.OrdinalIgnoreCase));
                if (buffer != null) 
                {
                    if (buffer.Properties.ContainsProperty(BufferProperties.TokenSpans))
                    {
                        var classificationInfos = buffer.Properties[BufferProperties.TokenSpans] as List<ClassificationInfo>;
                        if (classificationInfos != null)
                        {
                            var textSelection = dte2.ActiveDocument.Selection as TextSelection;
                            if (textSelection != null)
                            {
                                var token =
                                    classificationInfos.FirstOrDefault(
                                        m => textSelection.ActivePoint.AbsoluteCharOffset >= m.Start &&
                                        textSelection.ActivePoint.AbsoluteCharOffset <= (m.Start + m.Length) );

                                if (token.ClassificationType.Classification == Classifications.PowerShellCommand)
                                {
                                    _fileName = fileName;
                                    _offset = textSelection.ActivePoint.AbsoluteCharOffset;
                                    buffer.Properties.ContainsProperty(BufferProperties.Ast);
                                    {
                                        _ast = buffer.Properties[BufferProperties.Ast] as Ast;
                                    }
                                    bVisible = true;
                                }
                            }
                        }
                    }
                }
            }

            var menuItem = sender as OleMenuCommand;
            if (menuItem != null)
            {
                menuItem.Visible = bVisible;
                menuItem.Supported = bVisible;
                menuItem.Enabled = bVisible;
            }
        }
    }
}
