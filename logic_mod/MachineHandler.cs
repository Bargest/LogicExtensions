using Logic.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic
{
    public class MachineHandler : IDisposable
    {
        Logic ModContext;
        public Dictionary<uint, HashSet<MExtKey>> AllGates;
        Dictionary<BlockBehaviour, CpuBlock> CpuBlocks = new Dictionary<BlockBehaviour, CpuBlock>();
        KeyInputController KeyInput;
        Machine machine;
        DateTime lastGc;

        Dictionary<uint, HashSet<MKey>> AllKeys;

        public MachineHandler(Machine m)
        {
            machine = m;
            lastGc = DateTime.Now;
            ModContext = SingleInstance<Logic>.Instance;
        }

        public struct BlockKey
        {
            public BlockBehaviour block;
            public MKey key;
            public int IOMode;
        }

        public void Collect()
        {
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
            return block.SelectMany(x => x.MapperTypes.Where(y => y is MKey).Select(y => new BlockKey { block = x, key = y as MKey, IOMode = CpuBlocks.ContainsKey(x) ? 2 : (y as MKey).isEmulator ? 1 : 0 }));
        }

        public IEnumerable<BlockKey> GetCpuKeys()
        {
            return CpuBlocks.SelectMany(x => x.Value.PIO.Values.Select(y => new BlockKey { block = x.Key, key = y, IOMode = 2 }));
        }

        public void AddKey(KeyInputController input, BlockBehaviour extLogic, MExtKey key)
        {
            foreach (var kk in key.UpdatedKeyCodes)
                input.AddMKey(extLogic, key, (KeyCode)kk.Value);
        }

        public void AddExtKeyEmulator(MExtKey key)
        {
            //foreach (var kk in key.UpdatedKeyCodes)
            foreach (var kk in key.ResolveKeys())
            {
                if (!AllGates.ContainsKey(kk))
                    AllGates[kk] = new HashSet<MExtKey>();

                AllGates[kk].Add(key);
            }
        }

        public IEnumerable<KeyValuePair<BlockBehaviour, CpuBlock>> GetCpus()
        {
            return CpuBlocks;
        }

        public CpuBlock GetCpuBlock(BlockBehaviour b)
        {
            return (CpuBlocks.ContainsKey(b) ? CpuBlocks[b] : null);
        }


        bool detectedGameArchChange = false;
        public int CrutchToGetEmulatorsCount(MKey key)
        {
            if (detectedGameArchChange)
                return key.Emulating ? 1 : 0; // It's totally invalid, but we need this fallback for future

            int count = 0;
            while (key.Emulating)
            {
                ++count;
                key.UpdateEmulation(false);
                if (count > 2000)
                {
                    // more than 2000 blocks are emulating the same key, it's strange
                    // so we disable this logic until restart to prevent hanging
                    detectedGameArchChange = true;
                    break;
                }
            }
            for (int i = 0; i < count; ++i)
                key.UpdateEmulation(true);
            return count;
        }

        private bool LegacyEmu(uint code)
        {
            bool legacyEmu = false;
            if (AllKeys.ContainsKey(code))
            {
                var newEmuCount = AllKeys[code].Where(x => (x is MExtKey)).Where(x => (x as MExtKey).IAmEmulating).Count();
                legacyEmu = AllKeys[code].Where(x => !(x is MExtKey) && x.isEmulator).Any(x => CrutchToGetEmulatorsCount(x) > newEmuCount);
            }
            return legacyEmu;
        }

        public float IsAnyEmulating(uint code)
        {
            if (LegacyEmu(code))
                return 1;

            if (!AllGates.ContainsKey(code))
                return 0;

            return AllGates[code].Max(x => x.OutValue);
        }

        public MExtKey GetExtEmulator(uint code)
        {
            if (!AllGates.ContainsKey(code))
                return null;

            return AllGates[code].OrderByDescending(x => x.OutValue).FirstOrDefault();
        }

        public bool IsNativeHeld(MKey key)
        {
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

        public IEnumerable<uint> GetMKeys(MKey key)
        {
            if (key is MExtKey mk)
            {
                for (int i = 0; i < key.KeysCount; ++i)
                    yield return mk.ResolveKey(i);
            }
            else
            {
                for (int i = 0; i < key.KeysCount; ++i)
                    yield return (uint)key.GetKey(i);
            }
        }

        public float ReadValue(MKey key)
        {
            if (IsNativeHeld(key))
                return 1;

            return GetMKeys(key).Select(x => IsAnyEmulating(x)).DefaultIfEmpty().Max();
        }

        public MExtKey GetExtEmulator(MKey key)
        {
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
            AllGates = new Dictionary<uint, HashSet<MExtKey>>();
            AllKeys = GetKeys(machine.SimulationBlocks).SelectMany(x => GetMKeys(x.key).Select(y => new { x.key, code = y }))
                .GroupBy(x => x.code, x => x.key)
                .ToDictionary(x => x.Key, x => new HashSet<MKey>(x));
            KeyInput = machine.GetComponent<KeyInputController>();
            foreach (var block in machine.SimulationBlocks)
            {
                ModContext.PlaceAdditionScripts(block);
                if (CpuBlocks.ContainsKey(block))
                    ModContext.Registers[typeof(CpuBlock)](block, KeyInput);
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
