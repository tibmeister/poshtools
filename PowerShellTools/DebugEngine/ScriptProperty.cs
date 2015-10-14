using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

using System.Collections.ObjectModel;
using PowerShellTools.Common.Logging;
using PowerShellTools.Common.ServiceManagement.DebuggingContract;

namespace PowerShellTools.DebugEngine
{
    /// <summary>
    /// Returns a <see cref="ScriptProperty"/> based on the type of the variable specified by <paramref name="name"/>
    /// </summary>
    public class ScriptPropertyFactory
    {
        public static ScriptProperty MakeProperty(ScriptDebugger debugger, Variable var, string parentPath)
        {
            if (var == null)
                return null;

            if (var.IsEnumerable && !var.Type.Contains("System.String"))
            {
                return new EnumerableScriptProperty(debugger, var, parentPath);
            }

            if (var.IsPSObject)
            {
                return new PSObjectScriptProperty(debugger, var, parentPath);
            }

            return new ScriptProperty(debugger, var.VarName, var.VarValue, var.IsEnum, var.Type, parentPath);
        }
    }

    /// <summary>
    /// Script property for enumerable types such as lists and arrays.
    /// </summary>
    public class EnumerableScriptProperty : ScriptProperty
    {
        private string _val;
        public EnumerableScriptProperty(ScriptDebugger debugger, Variable var, string parentPath)
            : base(debugger, var, parentPath)
        {
            _val = var.VarName;
        }

        protected override IEnumerable<ScriptProperty> GetChildren()
        {
            string varFullName = string.IsNullOrEmpty(_path) ? _val : string.Format("{0}\\{1}", _path, _val);

            Collection<Variable> expanded = _debugger.DebuggingService.GetExpandedIEnumerableVariable(varFullName);

            foreach (var item in expanded)
            {
                yield return ScriptPropertyFactory.MakeProperty(_debugger, item, varFullName);
            }
        }
    }

    /// <summary>
    /// Script property for PSObjects.
    /// </summary>
    public class PSObjectScriptProperty : ScriptProperty
    {
        private string _val;
        public PSObjectScriptProperty(ScriptDebugger debugger, Variable var, string parentPath)
            : base(debugger, var, parentPath)
        {
            _val = var.VarName;
        }

        protected override IEnumerable<ScriptProperty> GetChildren()
        {
            string varFullName = string.IsNullOrEmpty(_path) ? _val : string.Format("{0}\\{1}", _path, _val);

            Collection<Variable> propVars = _debugger.DebuggingService.GetPSObjectVariable(varFullName);

            foreach (var item in propVars)
            {
                yield return ScriptPropertyFactory.MakeProperty(_debugger, item, varFullName);
            }
        }

        public override string TypeName
        {
            get { return base.TypeName; }
        }
    }

    /// <summary>
    /// An implementation of IDebugProperty2 used to display variables in the local and watch windows.
    /// </summary>
    public class ScriptProperty : IDebugProperty2
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScriptProperty));

        public string Name { get; set; }

        public string Value { get; set; }

        public bool IsEnum { get; set; }

        public virtual String TypeName { get; set; }

        protected readonly ScriptDebugger _debugger;

        protected string _path { get; private set; }

        public ScriptProperty(ScriptDebugger debugger, string name, string value, bool isEnum, string type, string path)
        {
            Log.DebugFormat("{0} {1}", name, value);
            Name = name;
            Value = value;
            IsEnum = isEnum;
            _debugger = debugger;
            TypeName = (type == null ? string.Empty : type);
            _path = path;
        }

        protected ScriptProperty(ScriptDebugger debugger, Variable var, string path)
        {
            Log.DebugFormat("{0} {1}", var.VarName, var.VarValue);
            Name = var.VarName;
            Value = var.VarValue;
            IsEnum = var.IsEnum;
            _debugger = debugger;
            TypeName = (var.Type == null ? string.Empty : var.Type);
            _path = path;
        }

        #region Implementation of IDebugProperty2

        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            Log.DebugFormat("GetPropertyInfo [{0}]", dwFields);

            pPropertyInfo[0].dwFields = enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NONE;

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
            {
                pPropertyInfo[0].bstrName = Name;
                pPropertyInfo[0].bstrFullName = Name;
                pPropertyInfo[0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE) != 0)
            {
                pPropertyInfo[0].bstrValue = Value == null ? String.Empty : Value.ToString();
                pPropertyInfo[0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE) != 0)
            {
                pPropertyInfo[0].bstrType = TypeName;
                pPropertyInfo[0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE;
            }

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB) != 0)
            {
                pPropertyInfo[0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB;
                pPropertyInfo[0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_VALUE_READONLY;
                if (!ScriptPropertyCollection.PsBaseTypes.Contains(TypeName))
                {
                    pPropertyInfo[0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
                }
            }

            pPropertyInfo[0].pProperty = this;
            pPropertyInfo[0].dwFields |= enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP;

            return VSConstants.S_OK;
        }

        public int SetValueAsString(string pszValue, uint dwRadix, uint dwTimeout)
        {
            _debugger.SetVariable(Name, pszValue);
            return VSConstants.S_OK;
        }

        public int SetValueAsReference(IDebugReference2[] rgpArgs, uint dwArgCount, IDebugReference2 pValue, uint dwTimeout)
        {
            throw new NotImplementedException();
        }

        public int EnumChildren(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, ref Guid guidFilter, enum_DBG_ATTRIB_FLAGS dwAttribFilter, string pszNameFilter, uint dwTimeout, out IEnumDebugPropertyInfo2 ppEnum)
        {
            if (Value != null)
            {
                var props = GetChildren();
                ppEnum = new ScriptPropertyCollection(props.ToArray());
                return VSConstants.S_OK;
            }

            ppEnum = null;
            return VSConstants.S_FALSE;
        }

        protected virtual IEnumerable<ScriptProperty> GetChildren()
        {
            string varFullName = string.IsNullOrEmpty(_path) ? Name : string.Format("{0}\\{1}", _path, Name);

            Collection<Variable> propVars = _debugger.DebuggingService.GetObjectVariable(varFullName);

            foreach (var item in propVars)
            {
                yield return ScriptPropertyFactory.MakeProperty(_debugger, item, varFullName);
            }
        }

        public int GetParent(out IDebugProperty2 ppParent)
        {
            throw new NotImplementedException();
        }

        public int GetDerivedMostProperty(out IDebugProperty2 ppDerivedMost)
        {
            throw new NotImplementedException();
        }

        public int GetMemoryBytes(out IDebugMemoryBytes2 ppMemoryBytes)
        {
            throw new NotImplementedException();
        }

        public int GetMemoryContext(out IDebugMemoryContext2 ppMemory)
        {
            throw new NotImplementedException();
        }

        public int GetSize(out uint pdwSize)
        {
            throw new NotImplementedException();
        }

        public int GetReference(out IDebugReference2 ppReference)
        {
            throw new NotImplementedException();
        }

        public int GetExtendedInfo(ref Guid guidExtendedInfo, out object pExtendedInfo)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    /// A list of variables for a particular stack frame.
    /// </summary>
    public class ScriptPropertyCollection : List<ScriptProperty>, IEnumDebugPropertyInfo2
    {
        private uint _count;
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScriptPropertyCollection));
        public static List<string> PsBaseTypes = new List<string>() { "System.String", "System.Char", "System.Byte", "System.Int32", "System.Int64", "System.Boolean", "System.Decimal", "System.Single", "System.Double", "System.IntPtr" };

        public ScriptPropertyCollection(ScriptDebugger debugger)
        {
            Log.Debug("debugger");
            foreach (var keyVal in debugger.Variables)
            {
                var val = keyVal.Value;
                Add(ScriptPropertyFactory.MakeProperty(debugger, val, string.Empty));
            }
        }

        public ScriptPropertyCollection(params ScriptProperty[] children)
        {
            Log.Debug("children");
            foreach (var scriptProperty in children)
            {
                Add(scriptProperty);
            }
        }

        #region Implementation of IEnumDebugPropertyInfo2

        public int Next(uint celt, DEBUG_PROPERTY_INFO[] rgelt, out uint pceltFetched)
        {
            Log.Debug("Next, base at" + _count.ToString());
            for (var i = 0; i < celt && (int)(i +_count) < this.Count; i++)
            {
                rgelt[i].bstrName = this[(int)(i + _count)].Name;
                rgelt[i].bstrFullName = this[(int)(i + _count)].Name;
                rgelt[i].bstrValue = this[(int)(i + _count)].Value != null ? this[(int)(i + _count)].Value.ToString() : "$null";
                rgelt[i].bstrType = this[(int)(i + _count)].TypeName != null ? this[(int)(i + _count)].TypeName : String.Empty;
                rgelt[i].pProperty = this[(int)(i + _count)];
                rgelt[i].dwAttrib = GetExpandableAttributesOverride(this[(int)(i + _count)]) 
                    & GetExpandableAttributes(this[(int)(i + _count)].TypeName);
                rgelt[i].dwFields = enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME |
                                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_VALUE |
                                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_TYPE |
                                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_PROP |
                                    enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_ATTRIB;
            }
            pceltFetched = celt;
            _count += celt;
            return VSConstants.S_OK;

        }

        private enum_DBG_ATTRIB_FLAGS GetExpandableAttributesOverride(ScriptProperty prop)
        {
            return prop.IsEnum ? 0 : enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
        }

        private enum_DBG_ATTRIB_FLAGS GetExpandableAttributes(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return 0;
            }

            if (PsBaseTypes.Contains(typeName))
            {
                return 0;
            }

            return enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
        }

        public int Skip(uint celt)
        {
            Log.Debug("Skip");
            _count += celt;
            return VSConstants.S_OK;
        }

        public int Reset()
        {
            Log.Debug("Reset");
            _count = 0;
            return VSConstants.S_OK;
        }

        public int Clone(out IEnumDebugPropertyInfo2 ppEnum)
        {
            Log.Debug("Clone");
            ppEnum = null;
            return VSConstants.E_NOTIMPL;
        }

        public int GetCount(out uint pcelt)
        {
            Log.Debug("GetCount");
            pcelt = (uint)this.Count;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
