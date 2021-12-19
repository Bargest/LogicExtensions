using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    public class MExtKey : MKey
    {
        public const KeyCode KeyPlaceHolder = KeyCode.Break;
        public readonly static HashSet<uint> KnownKeys = new HashSet<uint>(Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().Select(x => (uint)x));
        
        public BlockBehaviour Parent { get; private set; }
        public MText Text;
        public Dictionary<int, uint> UpdatedKeyCodes = new Dictionary<int, uint>();
        public Dictionary<int, uint> LoadUpdatedKeyCodes = new Dictionary<int, uint>();
        bool freezeUpdate = false;
        bool registeringKeys = false;
        Func<uint, float> MachineEmulating;

        public MExtKey(int nameLocalisationId, string key, KeyCode defaultKey, BlockBehaviour parent, bool isEmulator = false)
            : base(nameLocalisationId, key, defaultKey, isEmulator)
        {
            Parent = parent;
            Text = new MText(nameLocalisationId, key + "_text", "");
            Text.TextChanged += Text_TextChanged;
            KeysChanged += MExtKey_KeysChanged;
        }

        public MExtKey(string displayName, string key, KeyCode defaultKey, BlockBehaviour parent, bool isEmulator = false)
            : base(displayName, key, defaultKey, isEmulator)
        {
            Parent = parent;
            Text = new MText(displayName, key + "_text", "");
            Text.TextChanged += Text_TextChanged;
            KeysChanged += MExtKey_KeysChanged;
        }

        public uint ResolveKey(int id)
        {
            if (UpdatedKeyCodes.ContainsKey(id))
                return UpdatedKeyCodes[id];
            return (uint)GetKey(id);
        }

        private void MExtKey_KeysChanged()
        {
            if (Game.IsSimulating || registeringKeys)
                return;

            for (int i = 0; i < KeysCount; ++i)
                if (GetKey(i) != KeyPlaceHolder)
                    UpdatedKeyCodes.Remove(i);

            UpdateText();
            //Debug.Log("Key changed: " + Text.Value);
        }

        public IEnumerable<uint> ParseText(string value, uint[] origKeys)
        {
            var keyParts = value.Split(';');
            var cnt = keyParts.Length > origKeys.Length ? origKeys.Length : keyParts.Length;
            for (int i = 0; i < cnt; ++i)
            {
                if (uint.TryParse(keyParts[i], out uint val))
                {
                    yield return val;
                }
                else
                {
                    if (KeyCodeConverter.GetKey(keyParts[i], out KeyCode code))
                        yield return (uint)code;
                    else
                        yield return origKeys[i];
                }
            }
        }

        public void SetText(string value)
        {
            var origKeys = ResolveKeys().ToArray();
            while (KeysCount > 0)
                RemoveKey(0);

            try
            {
                freezeUpdate = true; // prevent recursion! Or we will get loop as Text.Change -> Key.Change -> Text.Change -> ...
                foreach (var val in ParseText(value, origKeys))
                {
                    if (KnownKeys.Contains(val))
                    {
                        var id = AddKey((KeyCode)val);
                        UpdatedKeyCodes.Remove(id);
                    }
                    else
                    {
                        var id = AddKey(KeyPlaceHolder);
                        UpdatedKeyCodes[id] = val;
                    }
                }
                UpdateText();
                InvokeKeysChanged();
            }
            finally
            {
                freezeUpdate = false;
            }
        }

        public void Text_TextChanged(string value)
        {
            if (Game.IsSimulating || freezeUpdate)
                return;

            SetText(value);
        }

        public override void ApplyValue()
        {
            base.ApplyValue();
            LoadUpdatedKeyCodes = UpdatedKeyCodes.ToDictionary(x => x.Key, x => x.Value);
        }

        public IEnumerable<uint> ResolveKeys()
        {
            for (int i = 0; i < KeysCount; ++i)
                yield return ResolveKey(i);
        }

        public string SerializeKey(uint x)
        {
            if (!KnownKeys.Contains(x))
                return x.ToString();
            try
            {
                return KeyCodeConverter.GetKey((KeyCode)x);
            }
            catch
            {
                return x.ToString();
            }
        }

        public string GenerateText()
        {
            return string.Join(";", ResolveKeys().Select(x => SerializeKey(x)).ToArray());
        }

        private void UpdateText()
        {
            Text.Value = GenerateText();
        }

        public override XData Serialize()
        {
            string[] array = new string[KeysCount + (ignored ? 1 : 0)];
            int i;
            for (i = 0; i < KeysCount; i++)
                array[i] = SerializeKey(ResolveKey(i));
            if (ignored)
                array[i] = "Ignored=" + ignored;
            return new XStringArray("bmt-" + Key, array);
        }

        public override XData SerializeLoadValue()
        {
            var saved = ((string[])(base.SerializeLoadValue() as XStringArray));
            var last = saved.LastOrDefault();
            var loadIgnored = last != null && last.ToLower().StartsWith("ignored=true");

            string[] array = new string[LoadKeysCount + (loadIgnored ? 1 : 0)];
            int i;
            for (i = 0; i < LoadKeysCount; i++)
                array[i] = SerializeKey(ResolveKey(i));

            if (loadIgnored)
                array[i] = "Ignored=" + loadIgnored;
            return new XStringArray("bmt-" + Key, array);
        }

        public override XData SerializeDefault()
        {
            Debug.LogWarning($"Serialize default");
            return base.SerializeDefault();
        }

        public void CopyFrom(MExtKey other)
        {
            // we blindly assume other.Text is correct
            if (Text.Value != other.Text.Value)
                SetText(other.Text.Value);
        }

        public override void DeSerialize(XData raw)
        {
            var keyArray = (string[])(XStringArray)raw;
            var newKeyArray = new string[keyArray.Length];
            // replace bad keys
            for (int i = 0; i < keyArray.Length; ++i)
            {
                if (uint.TryParse(keyArray[i], out uint keyId) && !KnownKeys.Contains(keyId))
                    newKeyArray[i] = KeyCodeConverter.GetKey(KeyPlaceHolder);
                else
                    newKeyArray[i] = keyArray[i];
            }

            base.DeSerialize(new XStringArray("", newKeyArray));
            for (int i = 0; i < keyArray.Length; ++i)
            {
                var key = keyArray[i];
                if (!uint.TryParse(key, out uint extKey) || KnownKeys.Contains(extKey))
                    continue;

                UpdatedKeyCodes[i] = extKey;
            }
            UpdateText();
            InvokeKeysChanged();
        }

        public void SetKeycodes(KeyInputController input, Func<uint, float> machineEmu)
        {
            registeringKeys = true;
            try
            {
                InputController = input;
                MachineEmulating = machineEmu;
                foreach (var kp in UpdatedKeyCodes)
                    AddOrReplaceKey(kp.Key, (KeyCode)kp.Value);
                InvokeKeysChanged();
            }
            finally
            {
                registeringKeys = false;
            }
        }

        public void ResetKeycodes()
        {
            registeringKeys = true;
            try
            {
                InputController = null;
                MachineEmulating = null;
                foreach (var kp in UpdatedKeyCodes)
                    AddOrReplaceKey(kp.Key, KeyPlaceHolder);
                InvokeKeysChanged();
            }
            finally
            {
                registeringKeys = false;
            }
        }

        public KeyInputController InputController;
        public float OutValue { get; private set; }
        public bool IAmEmulating { get; private set; }

        public void SetOutValue(BlockBehaviour b, float v)
        {
            if (OutValue == v || v != 0 && Mathf.Approximately(OutValue, v))
                return;

            OutValue = v;
            bool needEmulate = OutValue > 0;
            if (needEmulate != IAmEmulating)
            {
                IAmEmulating = needEmulate;
                InputController.Emulate(b, new MKey[0], this, needEmulate);
            }
        }

        float fixedTime;
        bool wasEmulating, temp;

        public bool EmuHeld()
        {
            return (ResolveKeys().Any(x => MachineEmulating(x) > 0));
        }

        public bool CheckEmulation(bool condition, bool lastCondition)
        {
            if (fixedTime != Time.fixedTime)
            {
                wasEmulating = temp;
                fixedTime = Time.fixedTime;
            }
            temp = EmuHeld();
            if (temp == condition && wasEmulating == lastCondition)
            {
                return true;
            }
            return false;
        }

        public bool EmuPressed()
        {
            return CheckEmulation(true, false);
        }

    }

}
