using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Design.Serialization;

namespace PowerShellTools.LanguageService
{
    [Export("PowerShellCodeDomProvider", typeof(CodeDomProvider))]
    class PowerShellCodeDomProvider : CodeDomProvider
    {
        public override ICodeGenerator CreateGenerator()
        {
            return new PowerShellCodeGenerator();
        }

        public override ICodeCompiler CreateCompiler()
        {
            throw new NotImplementedException();
        }

        public override CodeCompileUnit Parse(TextReader codeStream)
        {
            var docDataTextReader = codeStream as DocDataTextReader;
            var fileName = docDataTextReader.GetDesignerFileName();

            if (!File.Exists(fileName))
            {
                using (var textWriter = new StreamWriter(fileName))
                {
                    textWriter.WriteLine("$MainForm = New-Object System.Windows.Forms.Form");
                }
            }
                
            using (var textReader = new StreamReader(fileName))
            {
                var script = textReader.ReadToEnd();
                var ast = ScriptBlock.Create(script).Ast;
                var visitor = new CodeDomAstVisitor();

                var unit = ast.Visit(visitor) as CodeCompileUnit;

                
                unit.ReferencedAssemblies.Add(typeof(Point).Assembly.FullName);
                unit.ReferencedAssemblies.Add(typeof(Form).Assembly.FullName);

                Debug.WriteLine("Generated Unit");

    #if DEBUG
                var generator = new DebugCodeGenerator();
                generator.GenerateCodeFromCompileUnit(unit, null, null);
    #endif

                return unit;
            }
        }
    }

    public class PowerShellCodeGenerator : ICodeGenerator
    {
        public TypeModel _currentType;

        private string _ps1FileName;

        public bool IsValidIdentifier(string value)
        {
            return true;
        }

        public void ValidateIdentifier(string value)
        {

        }

        public string CreateEscapedIdentifier(string value)
        {
            throw new NotImplementedException();
        }

        public string CreateValidIdentifier(string value)
        {
            //Here have some documentation:
            //CreateValidIdentifier tests whether the identifier conflicts with reserved or language keywords, and returns a valid identifier name that does not conflict. The returned identifier will contain the same value but, if it conflicts with reserved or language keywords, will have escape code formatting added to differentiate the identifier from the keyword. Typically, if the value needs modification, value is returned preceded by an underscore "_".

            return value;
        }

        public string GetTypeOutput(CodeTypeReference type)
        {
            throw new NotImplementedException();
        }

        public bool Supports(GeneratorSupport supports)
        {
            return true;
        }

        public void GenerateCodeFromExpression(CodeExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            if (e is CodeObjectCreateExpression)
            {
                GenerateCodeFromObjectCreateExpression(e as CodeObjectCreateExpression, w, o);
            }

            if (e is CodePrimitiveExpression)
            {
                GenerateCodeFromPrimitiveExpression(e as CodePrimitiveExpression, w, o);
            }

            if (e is CodeVariableReferenceExpression)
            {
                GenerateCodeFromVariableReferenceExpression(e as CodeVariableReferenceExpression, w, o);
            }

            if (e is CodePropertyReferenceExpression)
            {
                GenerateCodeFromPropertyReferenceExpression(e as CodePropertyReferenceExpression, w, o);
            }

            if (e is CodeFieldReferenceExpression)
            {
                GenerateCodeFromFieldReferenceExpression(e as CodeFieldReferenceExpression, w, o);
            }

            if (e is CodeThisReferenceExpression)
            {
                GenerateCodeFromThisReferenceExpression(e as CodeThisReferenceExpression, w, o);
            }

            if (e is CodeMethodInvokeExpression)
            {
                GenerateCodeFromMethodInvokeExpression(e as CodeMethodInvokeExpression, w, o);
            }

            if (e is CodeDelegateCreateExpression)
            {
                GenerateCodeFromDelegateCreateExpression(e as CodeDelegateCreateExpression, w);
            }

            if (e is CodeTypeReferenceExpression)
            {
                GenerateCodeFromTypeReferenceExpression(e as CodeTypeReferenceExpression, w);
            }
        }

        private void GenerateCodeFromTypeReferenceExpression(CodeTypeReferenceExpression e, TextWriter w)
        {
            w.Write("[" + e.Type.BaseType + "]");
        }

        private void GenerateCodeFromDelegateCreateExpression(CodeDelegateCreateExpression e, TextWriter w)
        {
            w.Write("$" + e.MethodName);    
        }

        private void GenerateCodeFromMethodInvokeExpression(CodeMethodInvokeExpression e, TextWriter w,
            CodeGeneratorOptions o)
        {
            var needsParameters = false;
            if (e.Method.TargetObject is CodeThisReferenceExpression)
            {
                if (_currentType.Methods.All(m => m.Name != e.Method.MethodName))
                {
                    w.Write("$MainForm.");
                    needsParameters = true;
                }
                else
                {
                    w.Write(". ");
                }
            }
            else
            {
                GenerateCodeFromExpression(e.Method.TargetObject, w, o);
                w.Write("." );
                needsParameters = true;
            }

            w.Write(e.Method.MethodName);

            int count = e.Parameters.Count;
            if (count != 0 || needsParameters)
            {
                w.Write("(");
            }

            foreach (CodeExpression expression in e.Parameters)
            {
                count--;
                GenerateCodeFromExpression(expression, w, o);
                if (count > 0)
                {
                    w.Write(",");
                }
            }

            if (e.Parameters.Count != 0 || needsParameters)
            {
                w.Write(")");    
            }

            w.Write("\r\n");
        }

        private void GenerateCodeFromThisReferenceExpression(CodeThisReferenceExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            w.Write("$MainForm");
        }

        private void GenerateCodeFromFieldReferenceExpression(CodeFieldReferenceExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            if (e.TargetObject is CodeThisReferenceExpression)
            {
                if (_currentType.Properties.All(m => m.Name != e.FieldName))
                {
                    w.Write("$MainForm.");
                }
                w.Write("$" + e.FieldName);
            }
            else if (e.TargetObject is CodeTypeReferenceExpression)
            {
                GenerateCodeFromExpression(e.TargetObject, w, o);
                w.Write("::" + e.FieldName);
            }
            else
            {
                GenerateCodeFromExpression(e.TargetObject, w, o);
                w.Write("." + e.FieldName);
            }
        }

        private void GenerateCodeFromPropertyReferenceExpression(CodePropertyReferenceExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            if (e.TargetObject is CodeThisReferenceExpression)
            {
                if (_currentType.Properties.All(m => m.Name != e.PropertyName))
                {
                    w.Write("$MainForm.");
                }
                w.Write(e.PropertyName);
            }
            else if (e.TargetObject is CodeTypeReferenceExpression)
            {
                GenerateCodeFromExpression(e.TargetObject, w, o);
                w.Write("::" + e.PropertyName);
            }
            else
            {
                GenerateCodeFromExpression(e.TargetObject, w, o);
                w.Write("." + e.PropertyName);
            }
        }

        private void GenerateCodeFromVariableReferenceExpression(CodeVariableReferenceExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            w.Write("$" + e.VariableName);
        }

        private void GenerateCodeFromPrimitiveExpression(CodePrimitiveExpression e, TextWriter w, CodeGeneratorOptions o)
        {
            if (e.Value is string)
            {
                w.Write("'{0}'", e.Value);
            }
            else if (e.Value is bool)
            {
                w.Write(((bool)e.Value) ? "$true" : "$false");
            }
            else
            {
                w.Write("{0}", e.Value);
            }
        }

        private void GenerateCodeFromObjectCreateExpression(CodeObjectCreateExpression e, TextWriter w,
            CodeGeneratorOptions o)
        {
            w.Write("New-Object -TypeName " + e.CreateType.BaseType);

            if (e.Parameters.Count > 0)
            {
                w.Write(" -ArgumentList @(");
                int count = e.Parameters.Count;
                foreach (CodeExpression parameter in e.Parameters)
                {
                    count--;
                    GenerateCodeFromExpression(parameter, w, o);
                    if (count > 0)
                    {
                        w.Write(",");
                    }
                }
                w.Write(")");
            }
        }

        public void GenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o)
        {
            if (e is CodeAssignStatement)
            {
                GenereateCodeFromAssignStatement(e as CodeAssignStatement, w, o);
            }

            if (e is CodeCommentStatement)
            {
                GenerateCodeFromCommentState(e as CodeCommentStatement, w, o);
            }

            if (e is CodeExpressionStatement)
            {
                GenerateCodeFromExpression((e as CodeExpressionStatement).Expression, w, o);
            }

            if (e is CodeAttachEventStatement)
            {
                GenerateCodeFromCodeAttachEventStatement(e as CodeAttachEventStatement, w, o);
            }
        }

        private void GenerateCodeFromCodeAttachEventStatement(CodeAttachEventStatement e, TextWriter w,
            CodeGeneratorOptions o)
        {
            if (e.Event.TargetObject is CodeThisReferenceExpression)
            {
                if (_currentType.Methods.All(m => m.Name != e.Event.EventName))
                {
                    w.Write("$MainForm.");
                }
            }
            else
            {
                GenerateCodeFromExpression(e.Event.TargetObject, w, o);
                w.Write(".");
            }

            w.Write("add_{0}(", e.Event.EventName);
            GenerateCodeFromExpression(e.Listener, w, o);
            w.Write(")" + Environment.NewLine);
        }


        private void GenerateCodeFromCommentState(CodeCommentStatement e, TextWriter w, CodeGeneratorOptions o)
        {
            w.WriteLine("#" + e.Comment.Text);
        }

        private void GenereateCodeFromAssignStatement(CodeAssignStatement e, TextWriter w, CodeGeneratorOptions o)
        {
            GenerateCodeFromExpression(e.Left, w, o);
            w.Write(" = ");
            GenerateCodeFromExpression(e.Right, w, o);
            w.Write("\r\n");
        }

        public void GenerateCodeFromNamespace(CodeNamespace e, TextWriter w, CodeGeneratorOptions o)
        {
            foreach (CodeTypeDeclaration type in e.Types)
            {
                GenerateCodeFromType(type, w, o);
            }
        }

        public void GenerateCodeFromCompileUnit(CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o)
        {
#if DEBUG
            var dcg = new DebugCodeGenerator();
            dcg.GenerateCodeFromCompileUnit(e, null, null);
#endif

            var textWriter = w as DocDataTextWriter;
            var fileName = textWriter.GetDesignerFileName();
            _ps1FileName = textWriter.GetFileName();
            using (var writer = new StreamWriter(fileName))
            {
                foreach (var reference in e.ReferencedAssemblies)
                {
                    writer.WriteLine("[void][System.Reflection.Assembly]::Load('{0}')", reference);
                }

                foreach (CodeNamespace name in e.Namespaces)
                {
                    GenerateCodeFromNamespace(name, writer, o);
                }

                writer.Flush();
            }
        }

        public void GenerateCodeFromType(CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o)
        {
            _currentType = new TypeModel();

            if (e.BaseTypes.Count == 1)
            {
                w.WriteLine("$MainForm = New-Object -TypeName {0}", e.BaseTypes[0].BaseType);
                _currentType.Properties.Add(new MemberModel
                {
                    IsPublic = true,
                    Name = "base"
                });
            }

            CodeConstructor constructor = null;
            foreach (var member in e.Members)
            {
                if (member is CodeConstructor)
                {
                    constructor = member as CodeConstructor;
                }
                else if (member is CodeMemberMethod)
                {
                    GenerateCodeFromMethod(member as CodeMemberMethod, w, o);    
                }
                else if (member is CodeMemberField)
                {
                    GenerateCodeFromField(member as CodeMemberField, w, o);
                }
            }

            if (constructor != null)
                GenerateCodeFromConstructor(constructor, w, o);
        }

        private void GenerateCodeFromConstructor(CodeConstructor e, TextWriter w, CodeGeneratorOptions o)
        {
            foreach (CodeStatement statement in e.Statements)
            {
                GenerateCodeFromStatement(statement, w, o);
            }
        }

        private void GenerateCodeFromMethod(CodeMemberMethod e, TextWriter w, CodeGeneratorOptions o)
        {
            if (e.UserData.Contains("DontGenerate")) return;

            //Must be event handler. Create in PS1 rather than Designer.ps1
            if (e.Name != "InitializeComponent")
            {
                AppendOrNoopToPs1(e.Name);
                return;
            }

            var method = new MemberModel();
            method.Name = e.Name;
            method.IsPublic = e.Attributes.HasFlag(MemberAttributes.Public);
            _currentType.Methods.Add(method);
            
            w.WriteLine("function " + e.Name);
            w.WriteLine("{");    
            
            foreach (CodeStatement statement in e.Statements)
            {
                GenerateCodeFromStatement(statement, w, o);
            }

            w.WriteLine("}");
        }

        private void AppendOrNoopToPs1(string functionName)
        {
            Token[] tokens;
            ParseError[] errors;
            var ast = Parser.ParseFile(_ps1FileName, out tokens, out errors);
            var functionExists = ast.FindAll(m => m is FunctionDefinitionAst, true)
                .Cast<FunctionDefinitionAst>()
                .Any(m => m.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase));

            if (functionExists) return;

            var ps1 = File.ReadAllText(_ps1FileName);
            ps1 = string.Format("${0} = {{{1}{1}}}{1}{1}{2}", functionName, Environment.NewLine, ps1);
            
            using (var textWriter = new StreamWriter(_ps1FileName))
            {
                textWriter.Write(ps1);
            }
        }

        private void GenerateCodeFromField(CodeMemberField e, TextWriter w, CodeGeneratorOptions o)
        {
            var member = new MemberModel();
            member.Name = e.Name;
            member.IsPublic = e.Attributes.HasFlag(MemberAttributes.Public);
            _currentType.Properties.Add(member);

            if (e.Type != null && e.Type.BaseType != "System.Void")
            {
                w.Write("[{0}]", e.Type.BaseType);
            }

            w.Write("$" + e.Name);

            if (e.InitExpression != null)
            {
                w.Write(" = ");
                GenerateCodeFromExpression(e.InitExpression, w, o);
                w.Write("\r\n");
            }
            else
            {
                w.Write(" = $null\r\n");
            }
        }
    }

    public class TypeModel
    {
        public TypeModel()
        {
            Methods = new List<MemberModel>();
            Properties = new List<MemberModel>();
        }

        public List<MemberModel> Methods { get; private set; }
        public List<MemberModel> Properties { get; private set; }
    }

    public class MemberModel
    {
        public bool IsPublic { get; set; }
        public string Name { get; set; }
    }
}
