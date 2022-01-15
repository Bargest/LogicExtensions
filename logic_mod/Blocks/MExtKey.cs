using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks
{
    public struct MappedKeyItem
    {
        public KeyCode Key;
        public string Msg;

        public bool IsKey => Msg == null;

        public MappedKeyItem(KeyCode key)
        {
            Key = key;
            Msg = null;
        }
        public MappedKeyItem(string msg)
        {
            Key = KeyCode.None;
            Msg = msg;
        }

        public override string ToString()
        {
            return IsKey ? Key.ToString() : Msg;
        }

        public override int GetHashCode()
        {
            return 17 * 31 + Key.GetHashCode() * 31 +  (Msg == null ? 0 : Msg.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return obj is MappedKeyItem other && (Key == other.Key && Msg == other.Msg);
        }
    }

    public class MExtKey : MKey
    {
        readonly static HashSet<uint> KnownKeys = new HashSet<uint>(Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().Select(x => (uint)x));
        
        public BlockBehaviour Parent { get; private set; }
        Func<MappedKeyItem, float> MachineEmulating;
        bool saveUseMessage = false;

        public MExtKey(int nameLocalisationId, string key, KeyCode defaultKey, BlockBehaviour parent, bool isEmulator = false)
            : base(nameLocalisationId, key, defaultKey, isEmulator)
        {
            Parent = parent;
            KeysChanged += MExtKey_KeysChanged;
        }

        public MExtKey(string displayName, string key, KeyCode defaultKey, BlockBehaviour parent, bool isEmulator = false)
            : base(displayName, key, defaultKey, isEmulator)
        {
            Parent = parent;
            KeysChanged += MExtKey_KeysChanged;
        }

        /*public MappedKeyItem ResolveKey(int id)
        {
            if (id < base.KeysCount)
                return new MappedKeyItem(GetKey(id));

            return (uint)GetKey(id);
        }*/

        private void MExtKey_KeysChanged()
        {
            saveUseMessage = useMessage;
            /*
            if (Game.IsSimulating || registeringKeys)
                return;

            for (int i = 0; i < KeysCount; ++i)
                if (GetKey(i) != KeyPlaceHolder)
                    UpdatedKeyCodes.Remove(i);

            UpdateText();*/
            //Debug.Log("Key changed: " + Text.Value);
        }

        bool IsKey(string value)
        {
            return KeyCodeConverter.GetKey(value, out KeyCode code);
        }

        KeyCode ToKey(string value)
        {
            if (!KeyCodeConverter.GetKey(value, out KeyCode code))
                throw new Exception("Invalid key " + value); // should never happen
            return code;
        }

        public string GenerateText()
        {
            return string.Join("|", ResolveKeys().Select(x => x.ToString()).ToArray());
        }

        public void SetTextDirect(string value)
        {
            if (Game.IsSimulating)
                return;

            var keyArray = value.Split('|');

            var newKeyArray = new List<KeyCode>();
            var msgs = new List<string>();
            for (int i = 0; i < keyArray.Length; ++i)
            {
                if (IsKey(keyArray[i]))
                    newKeyArray.Add(ToKey(keyArray[i]));
                else
                    msgs.Add(keyArray[i]);
            }

            message = msgs.ToArray();
            while (KeysCount > 0)
                RemoveKey(0);
            for (var k = 0; k < newKeyArray.Count; ++k)
                AddKey(newKeyArray[k]);
        }

        public IEnumerable<MappedKeyItem> ResolveKeys()
        {
            for (int i = 0; i < KeysCount; ++i)
                yield return new MappedKeyItem(GetKey(i));
            foreach (var msg in message.Where(x => !string.IsNullOrEmpty(x)))
                yield return new MappedKeyItem(msg);
        }

        public void CopyFrom(MExtKey other)
        {
            // we blindly assume other.Text is correct
            message = other.message;
            while (KeysCount > 0)
                RemoveKey(0);
            for (var k = 0; k < other.KeysCount; ++k)
                AddKey(other.GetKey(k));
        }

        public override void DeSerialize(XData raw)
        {
            var strArr = (XStringArray)raw;
            var keyArray = (string[])strArr;
            var newKeyArray = new List<string>();
            var msgs = new List<string>();
            for (int i = 0; i < keyArray.Length; ++i)
            {
                if (uint.TryParse(keyArray[i], out uint keyId) && keyId > KnownKeys.Max())
                    msgs.Add(keyArray[i]);
                else 
                    newKeyArray.Add(keyArray[i]);
            }

            if (msgs.Count > 0)
                newKeyArray.Add("Message=" + string.Join(";", msgs.ToArray()));

            var converted = newKeyArray.ToArray();
            base.DeSerialize(new XStringArray(strArr.Key, converted));
            saveUseMessage = useMessage;
        }

        public void SetUseMessage(bool fl)
        {
            useMessage = fl;
        }

        public void RestoreSavedUseMessage()
        {
            useMessage = saveUseMessage;
        }

        public void SetKeycodes(KeyInputController input, Func<MappedKeyItem, float> machineEmu)
        {
            try
            {
                InputController = input;
                MachineEmulating = machineEmu;
                //foreach (var kp in UpdatedKeyCodes)
                //    AddOrReplaceKey(kp.Key, (KeyCode)kp.Value);
                InvokeKeysChanged();
            }
            finally
            {
            }
        }

        public void ResetKeycodes()
        {
            try
            {
                RestoreSavedUseMessage();
                InputController = null;
                MachineEmulating = null;
                //foreach (var kp in UpdatedKeyCodes)
                //    AddOrReplaceKey(kp.Key, KeyPlaceHolder);
                InvokeKeysChanged();
            }
            finally
            {
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
                // emulate both key and message via dirty hack
                SetUseMessage(false);
                InputController.Emulate(b, new MKey[0], this, needEmulate);
                SetUseMessage(true);
                InputController.Emulate(b, new MKey[0], this, needEmulate);
                RestoreSavedUseMessage();
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
