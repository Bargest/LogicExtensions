using Logic.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic
{
    /// <summary>
    /// MachineHandler is an input manager for a specific machine
    /// In terms of Besiege it can be roughly assumed to be a kind of InputController
    /// </summary>
    public class MachineHandler : IDisposable
    {
        public struct BlockKey
        {
            public BlockBehaviour block;
            public MKey key;
            public int IOMode;
        }

        Logic ModContext;
        public Dictionary<string, HashSet<MExtKey>> AllGates;
        Dictionary<BlockBehaviour, CpuBlock> CpuBlocks = new Dictionary<BlockBehaviour, CpuBlock>();
        KeyInputController KeyInput;
        Machine machine;
        DateTime lastGc;

        Dictionary<string, HashSet<MKey>> AllKeys;

        public MachineHandler(Machine m)
        {
            machine = m;
            lastGc = DateTime.Now;
            ModContext = SingleInstance<Logic>.Instance;
        }

        public void Collect()
        {
            // Unfortunately I couldn't find a normal way to catch block destroy event
            // Since CPU has execution thread that must be stopped, I had to implement an easy Garbage Collector
            // Honestly I should rewrite CPU so it does not use threads, but this needs to implement own stack machine and I'm too lazy
            if (DateTime.Now - lastGc > TimeSpan.FromSeconds(10))
            {
                var hs = new HashSet<BlockBehaviour>(machine.SimulationBlocks.Concat(machine.BuildingBlocks));
                var list = CpuBlocks.Where(x => !hs.Contains(x.Key)).Select(x => x.Value).ToList();
                foreach (var b in list)
                {
                    RemoveCpuBlock(b);
                    b.Interp.Dispose();
                }
                lastGc = DateTime.Now;
            }
        }

        public IEnumerable<BlockKey> GetKeys(IEnumerable<BlockBehaviour> block)
        {
            return block.SelectMany(x => x.MapperTypes.Where(y => y is MKey)
                    .Select(y => new BlockKey {
                        block = x,
                        key = y as MKey,
                        IOMode = CpuBlocks.ContainsKey(x) ? 2 : (y as MKey).isEmulator ? 1 : 0
                    })
                );
        }

        public IEnumerable<BlockKey> GetCpuKeys()
        {
            return CpuBlocks.SelectMany(x => x.Value.PIO.Values
                    .Select(y => new BlockKey { block = x.Key, key = y, IOMode = 2 })
                );
        }

        public void AddUpdatedKey(KeyInputController input, BlockBehaviour extLogic, MExtKey key)
        {
            //foreach (var kk in key.UpdatedKeyCodes)
            //    input.AddMKey(extLogic, key, (KeyCode)kk.Value);

            // force add BOTH old key AND message
            key.SetUseMessage(false);
            foreach (var kk in key.ResolveKeys().Where(x => x.IsKey))
                input.AddMKey(extLogic, key, kk.Key);
            key.SetUseMessage(true);
            input.AddMKey(extLogic, key, KeyCode.None);
            key.RestoreSavedUseMessage();
        }

        public void AddExtKeyEmulator(MExtKey key)
        {
            foreach (var kk in key.ResolveKeys())
            {
                var ks = kk.ToString();
                if (!AllGates.ContainsKey(ks))
                    AllGates[ks] = new HashSet<MExtKey>();

                AllGates[ks].Add(key);
            }
        }

        public IEnumerable<KeyValuePair<BlockBehaviour, CpuBlock>> GetCpus()
        {
            return CpuBlocks;
        }

        public CpuBlock GetCpuBlock(BlockBehaviour b)
        {
            // Once again, I couldn't find a normal way to get CpuBlock from BlockBehavior
            return (CpuBlocks.ContainsKey(b) ? CpuBlocks[b] : null);
        }

        private bool LegacyEmu(MappedKeyItem code)
        {
            // Check if specified code is emulated by any vanilla MKey in machine
            bool legacyEmu = false;
            var skey = code.ToString();
            if (AllKeys.ContainsKey(skey))
                legacyEmu = AllKeys[skey].Where(x => !(x is MExtKey) && x.isEmulator).Any(x => x.EmulationHeld(true));
            return legacyEmu;
        }

        public float IsAnyEmulating(MappedKeyItem code)
        {
            // If vanilla MKey emulates this code - return 1.0
            if (LegacyEmu(code))
                return 1;

            // If no MExtKey is mapped to this code - return 0.0
            var skey = code.ToString();
            if (!AllGates.ContainsKey(skey))
                return 0;

            // Get maximum value from MExtKeys, mapped to this code
            var res = AllGates[skey].Max(x => x.OutValue);
            return res;
        }

        public MExtKey GetExtEmulator(MappedKeyItem code)
        {
            // Get the "strongest" ExtKey, that is emulating current code
            var skey = code.ToString();
            if (!AllGates.ContainsKey(skey))
                return null;

            return AllGates[skey].OrderByDescending(x => x.OutValue).FirstOrDefault();
        }

        public bool IsNativeHeld(MKey key)
        {
            // If a key is pressed on hardware keyboard
            bool result = false;
            if (!key.Ignored)
            {
                for (int i = 0; i < key.KeysCount; i++)
                {
                    if (KeyInput.IsHeld(key.GetKey(i)))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        public IEnumerable<MappedKeyItem> GetMKeys(MKey key)
        {
            // Unified interface to get all Int codes from any Key and ExtKey
            if (key is MExtKey mk)
            {
                //for (int i = 0; i < key.KeysCount; ++i)
                //    yield return mk.ResolveKey(i);
                foreach (var k in mk.ResolveKeys())
                    yield return k;
            }
            else
            {
                if (key.useMessage)
                {
                    for (int i = 0; i < key.message.Length; ++i)
                        yield return new MappedKeyItem(key.message[i]);
                }
                else
                {
                    for (int i = 0; i < key.KeysCount; ++i)
                        yield return new MappedKeyItem(key.GetKey(i));
                }
            }
        }

        public float ReadValue(MKey key)
        {
            // Get max output value from all codes, used by MKey
            if (IsNativeHeld(key))
                return 1;

            return GetMKeys(key).Select(x => IsAnyEmulating(x)).DefaultIfEmpty().Max();
        }

        public MExtKey GetExtEmulator(MKey key)
        {
            // Get the "strongest" ExtKey, that is emulating any of codes from MKey
            // It is used to match emulated value with target object description in case of sensor
            return GetMKeys(key).Select(x => GetExtEmulator(x)).OrderByDescending(x => x.OutValue).FirstOrDefault();
        }

        public void AddCpuBlock(CpuBlock c)
        {
            CpuBlocks[c.BlockBehaviour] = c;
        }

        public void RemoveCpuBlock(CpuBlock c)
        {
            CpuBlocks.Remove(c.BlockBehaviour);
        }

        public void Start()
        {
            AllGates = new Dictionary<string, HashSet<MExtKey>>();
            AllKeys = GetKeys(machine.SimulationBlocks).SelectMany(x => GetMKeys(x.key).Select(y => new { x.key, code = y }))
                .GroupBy(x => x.code.ToString(), x => x.key).ToDictionary(x => x.Key, x => new HashSet<MKey>(x));
            KeyInput = machine.GetComponent<KeyInputController>();
            foreach (var block in machine.SimulationBlocks)
            {
                ModContext.PlaceAdditionScripts(block);

                // On simulation start Besiege collects key codes from all keys and really maps them to KeyCode as dict's key
                // This crashes block loading, because extended IDs cannot be serialized to KeyCode
                // But AFAIK this is the only place in game where KeyCode really needs to be a KeyCode, not just Int
                // So we set Int values after simulation start and clear them on end
                if (CpuBlocks.ContainsKey(block))
                    ModContext.Registers[typeof(CpuBlock)](block, KeyInput); // CpuBlock is a special case, because it is not BlockBehavior
                else if (ModContext.Registers.ContainsKey(block.GetType()))
                    ModContext.Registers[block.GetType()](block, KeyInput);
            }
        }

        public void Stop()
        {
            KeyInput = null;
            foreach (var block in machine.SimulationBlocks)
            {
                if (CpuBlocks.ContainsKey(block))
                    ModContext.Unregisters[typeof(CpuBlock)](block);
                else if (ModContext.Unregisters.ContainsKey(block.GetType()))
                    ModContext.Unregisters[block.GetType()](block);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
