using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.Windows.Design.Host;
using PowerShellTools.Classification;

namespace PowerShellTools.Project {
    class WpfEventBindingProvider : EventBindingProvider {
        private readonly PowerShellFileNode _powerShellFileNode;

        public WpfEventBindingProvider(PowerShellFileNode powerShellFileNode) {
            _powerShellFileNode = powerShellFileNode;
        }

        public override bool AddEventHandler(EventDescription eventDescription, string objectName, string methodName) {
            // we return false here which causes the event handler to always be wired up via XAML instead of via code.
            return false;
        }

        public override bool AllowClassNameForMethodName() {
            return true;
        }

        public override void AppendStatements(EventDescription eventDescription, string methodName, string statements, int relativePosition) {
            throw new NotImplementedException();
        }

        public override string CodeProviderLanguage {
            get { return "PowerShell"; }
        }

        private Ast GetAst()
        {
            var view = _powerShellFileNode.GetTextView();
            var textBuffer = _powerShellFileNode.GetTextBuffer();
            return textBuffer.Properties[BufferProperties.Ast] as Ast;
        }

        public override bool CreateMethod(EventDescription eventDescription, string methodName, string initialStatements) {
            // build the new method handler
            var view = _powerShellFileNode.GetTextView();
            var textBuffer = _powerShellFileNode.GetTextBuffer();
            var ast = GetAst();

            using (var edit = textBuffer.CreateEdit())
            {
                var text = BuildMethod(
                    eventDescription,
                    methodName,
                    string.Empty,
                    view.Options.IsConvertTabsToSpacesEnabled() ?
                        view.Options.GetIndentSize() :
                        -1);

                edit.Insert(ast.Extent.EndOffset, text);
                edit.Apply();
                return true;
            }
        }

        private static string BuildMethod(EventDescription eventDescription, string methodName, string indentation, int tabSize) {
            StringBuilder text = new StringBuilder();
            text.AppendLine(indentation);
            text.Append(indentation);
            text.Append("function ");
            text.Append(methodName);
            text.Append('{');
            text.Append("param(");
            foreach (var param in eventDescription.Parameters) {
                text.Append(", ");
                text.Append("$" + param.Name);
            }
            text.AppendLine(")");
            text.AppendLine("}");
            text.AppendLine();

            return text.ToString();
        }

        public override string CreateUniqueMethodName(string objectName, EventDescription eventDescription) {
            var name = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}_{1}", objectName, eventDescription.Name);
            int count = 0;
            while (IsExistingMethodName(eventDescription, name)) {
                name = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}_{1}{2}", objectName, eventDescription.Name, ++count);
            }
            return name;
        }

        public override IEnumerable<string> GetCompatibleMethods(EventDescription eventDescription)
        {
            var ast = GetAst();
            var paramCount = eventDescription.Parameters.Count() + 1;
            return ast.FindAll(m => m is FunctionDefinitionAst && ((FunctionDefinitionAst)m).Parameters.Count == paramCount, false).Cast<FunctionDefinitionAst>().Select(m => m.Name);
        }

        public override IEnumerable<string> GetMethodHandlers(EventDescription eventDescription, string objectName) {
            return new string[0];
        }

        public override bool IsExistingMethodName(EventDescription eventDescription, string methodName) {
            return FindMethod(methodName) != null;
        }

        private FunctionDefinitionAst FindMethod(string methodName)
        {
            var ast = GetAst();
            return ast.Find(m => m is FunctionDefinitionAst && ((FunctionDefinitionAst) m).Name == methodName, false) as FunctionDefinitionAst;
        }

        public override bool RemoveEventHandler(EventDescription eventDescription, string objectName, string methodName) {
            var method = FindMethod(methodName);
            if (method != null) {
                var view = _powerShellFileNode.GetTextView();
                var textBuffer = _powerShellFileNode.GetTextBuffer();

                // appending a method adds 2 extra newlines, we want to remove those if those are still
                // present so that adding a handler and then removing it leaves the buffer unchanged.

                using (var edit = textBuffer.CreateEdit()) {
                    int start = method.Extent.StartOffset - 1;

                    // eat the newline we insert before the method
                    while (start >= 0) {
                        var curChar = edit.Snapshot[start];
                        if (!Char.IsWhiteSpace(curChar)) {
                            break;
                        } else if (curChar == ' ' || curChar == '\t') {
                            start--;
                            continue;
                        } else if (curChar == '\n') {
                            if (start != 0) {
                                if (edit.Snapshot[start - 1] == '\r') {
                                    start--;
                                }
                            }
                            start--;
                            break;
                        } else if (curChar == '\r') {
                            start--;
                            break;
                        }

                        start--;
                    }

                    
                    // eat the newline we insert at the end of the method
                    int end = method.Extent.EndOffset;                    
                    while (end < edit.Snapshot.Length) {
                        if (edit.Snapshot[end] == '\n') {
                            end++;
                            break;
                        } else if (edit.Snapshot[end] == '\r') {
                            if (end < edit.Snapshot.Length - 1 && edit.Snapshot[end + 1] == '\n') {
                                end += 2;
                            } else {
                                end++;
                            }
                            break;
                        } else if (edit.Snapshot[end] == ' ' || edit.Snapshot[end] == '\t') {
                            end++;
                            continue;
                        } else {
                            break;
                        }
                    }

                    // delete the method and the extra whitespace that we just calculated.
                    edit.Delete(Span.FromBounds(start + 1, end));
                    edit.Apply();
                }

                return true;
            }
            return false;
        }

        public override bool RemoveHandlesForName(string elementName) {
            throw new NotImplementedException();
        }

        public override bool RemoveMethod(EventDescription eventDescription, string methodName) {
            throw new NotImplementedException();
        }

        public override void SetClassName(string className) {
        }

        public override bool ShowMethod(EventDescription eventDescription, string methodName) {
            var method = FindMethod(methodName);
            if (method != null) {
                var view = _powerShellFileNode.GetTextView();
                view.Caret.MoveTo(new Microsoft.VisualStudio.Text.SnapshotPoint(view.TextSnapshot, method.Extent.StartOffset));
                view.Caret.EnsureVisible();
                return true;
            }

            return false;
        }

        public override void ValidateMethodName(EventDescription eventDescription, string methodName) {
        }
    }
}
