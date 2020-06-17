﻿using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Jint.Native;
using Jint.Native.Global;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace Jint.Runtime.Environments
{
    /// <summary>
    ///     http://www.ecma-international.org/ecma-262/6.0/#sec-global-environment-records
    /// </summary>
    internal sealed class GlobalEnvironmentRecord : EnvironmentRecord
    {
        private readonly GlobalObject _global;
        private readonly DeclarativeEnvironmentRecord _declarativeRecord;
        private readonly ObjectEnvironmentRecord _objectRecord;
        private readonly HashSet<string> _varNames = new HashSet<string>();

        public GlobalEnvironmentRecord(Engine engine, GlobalObject global) : base(engine)
        {
            _global = global;
            _objectRecord = new ObjectEnvironmentRecord(engine, global, provideThis: false, withEnvironment: false);
            _declarativeRecord = new DeclarativeEnvironmentRecord(engine);
        }

        public ObjectInstance GlobalThisValue => _global;

        public override bool HasBinding(string name)
        {
            return (_declarativeRecord._hasBindings && _declarativeRecord.HasBinding(name)) || _objectRecord.HasBinding(name);
        }

        internal override bool TryGetBinding(
            in BindingName name,
            bool strict,
            out Binding binding,
            out JsValue value)
        {
            if (_declarativeRecord._hasBindings &&
                _declarativeRecord.TryGetBinding(name, strict, out binding, out value))
            {
                return true;
            }

            // we unwrap by name
            binding = default;
            value = default;

            // normal case is to find
            if (_global._properties._dictionary.TryGetValue(name.Key, out var property)
                && property != PropertyDescriptor.Undefined)
            {
                value = ObjectInstance.UnwrapJsValue(property, _global);
                return true;
            }

            return TryGetBindingForGlobalParent(name, out value, property);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryGetBindingForGlobalParent(
            in BindingName name,
            out JsValue value,
            PropertyDescriptor property)
        {
            value = default;

            var parent = _global._prototype;
            if (parent != null)
            {
                property = parent.GetOwnProperty(name.StringValue);
            }

            if (property == PropertyDescriptor.Undefined)
            {
                return false;
            }

            value = ObjectInstance.UnwrapJsValue(property, _global);
            return true;
        }

        /// <summary>
        ///     http://www.ecma-international.org/ecma-262/5.1/#sec-10.2.1.2.2
        /// </summary>
        public override void CreateMutableBinding(string name, bool canBeDeleted = false)
        {
            if (_declarativeRecord._hasBindings && _declarativeRecord.HasBinding(name))
            {
                ExceptionHelper.ThrowTypeError(_engine, name + " has already been declared");
            }

            _declarativeRecord.CreateMutableBinding(name, canBeDeleted);
        }

        public override void CreateImmutableBinding(string name, bool strict = true)
        {
            if (_declarativeRecord._hasBindings && _declarativeRecord.HasBinding(name))
            {
                ExceptionHelper.ThrowTypeError(_engine, name + " has already been declared");
            }

            _declarativeRecord.CreateImmutableBinding(name, strict);
        }

        public override void InitializeBinding(string name, JsValue value)
        {
            if (_declarativeRecord._hasBindings && _declarativeRecord.HasBinding(name))
            {
                _declarativeRecord.InitializeBinding(name, value);
            }
            else
            {
                if (!_global.Set(name, value))
                {
                    ExceptionHelper.ThrowTypeError(_engine);
                }
            }
        }

        public override void SetMutableBinding(string name, JsValue value, bool strict)
        {
            if (_declarativeRecord._hasBindings && _declarativeRecord.HasBinding(name))
            {
                _declarativeRecord.SetMutableBinding(name, value, strict);
            }
            else
            {
                // fast inlined path as we know we target global, otherwise would be
                // _objectRecord.SetMutableBinding(name, value, strict); 
                if (!_global.Set(name, value) && strict)
                {
                    ExceptionHelper.ThrowTypeError(_engine);
                }
            }
        }

        internal override void SetMutableBinding(in BindingName name, JsValue value, bool strict)
        {
            if (_declarativeRecord._hasBindings && _declarativeRecord.HasBinding(name.Key.Name))
            {
                _declarativeRecord.SetMutableBinding(name.Key.Name, value, strict);
            }
            else
            {
                // fast inlined path as we know we target global, otherwise would be
                // _objectRecord.SetMutableBinding(name, value, strict); 
                if (!_global.Set(name.Key, value) && strict)
                {
                    ExceptionHelper.ThrowTypeError(_engine);
                }
            }
        }

        public override JsValue GetBindingValue(string name, bool strict)
        {
            return _declarativeRecord._hasBindings && _declarativeRecord.HasBinding(name)
                ? _declarativeRecord.GetBindingValue(name, strict)
                : _objectRecord.GetBindingValue(name, strict);
        }

        public override bool DeleteBinding(string name)
        {
            if (_declarativeRecord._hasBindings && _declarativeRecord.HasBinding(name))
            {
                return _declarativeRecord.DeleteBinding(name);
            }

            if (_global.HasOwnProperty(name))
            {
                var status = _objectRecord.DeleteBinding(name);
                if (status)
                {
                    _varNames.Remove(name);
                }

                return status;
            }

            return true;
        }

        public override bool HasThisBinding()
        {
            return true;
        }

        public override bool HasSuperBinding()
        {
            return false;
        }

        public override JsValue WithBaseObject()
        {
            return Undefined;
        }

        public override JsValue GetThisBinding()
        {
            return _global;
        }

        public bool HasVarDeclaration(string name)
        {
            return _varNames.Contains(name);
        }

        public bool HasLexicalDeclaration(string name)
        {
            return _declarativeRecord.HasBinding(name);
        }

        public bool HasRestrictedGlobalProperty(string name)
        {
            var existingProp = _global.GetOwnProperty(name);
            if (existingProp == PropertyDescriptor.Undefined)
            {
                return false;
            }

            return !existingProp.Configurable;
        }

        public bool CanDeclareGlobalVar(string name)
        {
            if (_global._properties.ContainsKey(name))
            {
                return true;
            }

            return _global.Extensible;
        }

        public bool CanDeclareGlobalFunction(string name)
        {
            if (!_global._properties.TryGetValue(name, out var existingProp) 
                || existingProp == PropertyDescriptor.Undefined)
            {
                return _global.Extensible;
            }

            if (existingProp.Configurable)
            {
                return true;
            }

            if (existingProp.IsDataDescriptor() && existingProp.Writable && existingProp.Enumerable)
            {
                return true;
            }

            return false;
        }

        public void CreateGlobalVarBinding(string name, bool canBeDeleted)
        {
            var hasProperty = _global.HasOwnProperty(name);
            if (!hasProperty && _global.Extensible)
            {
                _objectRecord.CreateMutableBindingAndInitialize(name, Undefined, canBeDeleted);
            }

            _varNames.Add(name);
        }

        public void CreateGlobalFunctionBinding(string name, JsValue value, bool canBeDeleted)
        {
            var existingProp = _global.GetOwnProperty(name);

            PropertyDescriptor desc;
            if (existingProp == PropertyDescriptor.Undefined || existingProp.Configurable)
            {
                desc = new PropertyDescriptor(value, true, true, canBeDeleted);
            }
            else
            {
                desc = new PropertyDescriptor(value, PropertyFlag.None);
            }

            _global.DefinePropertyOrThrow(name, desc);
            _global.Set(name, value, false);
            _varNames.Add(name);
        }

        public override JsValue ImplicitThisValue()
        {
            return Undefined;
        }

        internal override string[] GetAllBindingNames()
        {
            return _global.GetOwnProperties().Select(x => x.Key.ToString()).ToArray();
        }

        public override bool Equals(JsValue other)
        {
            return ReferenceEquals(_objectRecord, other);
        }
    }
}