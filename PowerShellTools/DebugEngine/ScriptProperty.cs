using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using log4net;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;

namespace PowerShellTools.DebugEngine
{
    /// <summary>
    /// Returns a <see cref="ScriptProperty"/> based on the type of the variable specified by <paramref name="name"/>
    /// </summary>
    public class ScriptPropertyFactory
    {
        public static ScriptProperty MakeProperty(ScriptDebugger debugger, string name, object value)
        {
            var psObject = value as PSObject;

            if (psObject != null && psObject.BaseObject is IEnumerable && !psObject.TypeNames.Contains("System.String"))
            {
                return new EnumerableScriptProperty(debugger, name, psObject.BaseObject);
            }

            if (psObject != null)
            {
                return new PSObjectScriptProperty(debugger, name, value);
            }


            if (value is IEnumerable && !(value is string))
            {
                return new EnumerableScriptProperty(debugger, name, value);
            }

            return new ScriptProperty(debugger, name, value);
        }
    }

    /// <summary>
    /// Script property for enumerable types such as lists and arrays.
    /// </summary>
    public class EnumerableScriptProperty : ScriptProperty
    {
        private readonly IEnumerable _enumerable;

        public EnumerableScriptProperty(ScriptDebugger debugger, string name, object value) : base(debugger, name, value)
        {
            if (!(value is IEnumerable))
            {
                throw new ArgumentException("Value must be of type IEnumerable.");
            }

            _enumerable = value as IEnumerable;
        }

        protected override IEnumerable<ScriptProperty> GetChildren()
        {
            int i = 0;
            foreach (var item in _enumerable)
            {
                yield return ScriptPropertyFactory.MakeProperty(_debugger, String.Format("[{0}]", i), item);
                i++;
            }
        }
    }

    /// <summary>
    /// Script property for PSObjects.
    /// </summary>
    public class PSObjectScriptProperty : ScriptProperty
    {
        private readonly PSObject _psObject;

        public PSObjectScriptProperty(ScriptDebugger debugger, string name, object value) : base(debugger, name, value)
        {
            if (!(value is PSObject))
            {
                throw new ArgumentException("Value must be of type PSObject");
            }


            _psObject = value as PSObject;
        }

        protected override IEnumerable<ScriptProperty> GetChildren()
        {
            Runspace.DefaultRunspace = _debugger.Runspace;
            var proeprties = new List<ScriptProperty>();
            foreach (var prop in _psObject.Properties)
            {
                if (proeprties.Any(m => m.Name == prop.Name))
                {
                    continue;
                }

                object val;
                try
                {
                    val = prop.Value;
                }
                catch
                {
                    val = "Failed to evaluate value.";
                }

                proeprties.Add(ScriptPropertyFactory.MakeProperty(_debugger, prop.Name, val));
            }
            return proeprties;
        }

        public override string TypeName
        {
            get { return _psObject.BaseObject.GetType().ToString(); }
        }
    }

    /// <summary>
    /// An implementation of IDebugProperty2 used to display variables in the local and watch windows.
    /// </summary>
    public class ScriptProperty : IDebugProperty2
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (ScriptProperty));

        public string Name { get; set; }

        public object Value { get; set; }

        public virtual String TypeName
        {
            get
            {
                return Value == null ? String.Empty : Value.GetType().ToString();
            }
        }

        protected readonly ScriptDebugger _debugger;

        public ScriptProperty(ScriptDebugger debugger, string name, object value)
        {
            Log.DebugFormat("{0} {1}", name, value);
            Name = name;
            Value = value;
            _debugger = debugger;
        }

        #region Implementation of IDebugProperty2

        public int GetPropertyInfo(enum_DEBUGPROP_INFO_FLAGS dwFields, uint dwRadix, uint dwTimeout, IDebugReference2[] rgpArgs, uint dwArgCount, DEBUG_PROPERTY_INFO[] pPropertyInfo)
        {
            Log.DebugFormat("GetPropertyInfo [{0}]", dwFields);

            pPropertyInfo[0].dwFields = enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NONE;

            if ((dwFields & enum_DEBUGPROP_INFO_FLAGS.DEBUGPROP_INFO_NAME) != 0)
            {
                pPropertyInfo[0].bstrName = Name;
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
                pPropertyInfo[0].dwAttrib |= enum_DBG_ATTRIB_FLAGS.DBG_ATTRIB_OBJ_IS_EXPANDABLE;
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
            var props = Value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return props.Select(propertyInfo => ScriptPropertyFactory.MakeProperty(_debugger , propertyInfo.Name, propertyInfo.GetValue(Value, null)));
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
        private static readonly ILog Log = LogManager.GetLogger(typeof (ScriptPropertyCollection));

        public ScriptPropertyCollection(ScriptDebugger debugger)
        {
            Log.Debug("debugger");
            foreach (var keyVal in debugger.Variables)
            {
                var val = keyVal.Value;
                Add(ScriptPropertyFactory.MakeProperty(debugger, keyVal.Key, val));
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
            Log.Debug("Next");
            for (var i = 0; i < celt; i++)
            {
                rgelt[i].bstrName = this[(int)(i + _count)].Name;
                rgelt[i].bstrValue = this[(int)(i + _count)].Value != null ? this[(int)(i + _count)].Value.ToString() : "$null";
                rgelt[i].bstrType = this[(int)(i + _count)].Value != null ? this[(int)(i + _count)].Value.GetType().ToString() : String.Empty;
                rgelt[i].pProperty = this[(int)(i + _count)];
                rgelt[i].dwAttrib = GetAttributes(this[(int) (i + _count)].Value);
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

        private enum_DBG_ATTRIB_FLAGS GetAttributes(object obj)
        {
            if (obj == null)
            {
                return 0;
            }

            if (obj is string || obj is int || obj is char || obj is byte)
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
