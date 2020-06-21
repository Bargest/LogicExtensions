using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Interop;
using Logic.Script;
using mattmc3.dotmore.Collections.Generic;
using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Logic.Blocks.Api
{
    public class CpuApi : SingleInstance<CpuApi>
    {
        class ROApiDescription
        {
            public List<CpuApiProperty> StaticFields;
            public List<CpuApiProperty> InstanceFields;
        }

        Dictionary<string, CpuApiFunc> RootApi;
        Dictionary<string, ROApiDescription> ApiNamespaces = new Dictionary<string, ROApiDescription>();

        public override string Name => "CpuApi";

        public void Attach(CpuBlock block)
        {
            foreach (var api in RootApi)
                block.Interp.SetValue(api.Key, (t, a) => api.Value.Implementation(block, t, a));

            foreach (var kp in ApiNamespaces)
            {
                var obj = block.Interp.Global.Get(kp.Key) as ObjectInstance;
                if (obj != null)
                    throw new Exception($"Runtime error: namespace {kp.Key} already exists");

                obj = ApiObjectConstructor.CreateApiConstructor(block, kp.Key, kp.Value.StaticFields, kp.Value.InstanceFields);
                block.Interp.Global.Set(kp.Key, obj);
            }
            // Legacy crutch
            var math = block.Interp.Global.Get("Math") as ObjectInstance;
            math.Set("newton", block.Interp.Global.Get("MathExt").Get("newton"));
        }

        public void AddNamespace(string nsp, ApiDescription apiList)
        {
            if (string.IsNullOrEmpty(nsp))
                throw new Exception("Namespace cannot be empty");

            var apiDict = apiList.StaticFields.ToDictionary(x => x.Name);
            if (ApiNamespaces.ContainsKey(nsp))
                throw new Exception($"Namespace {nsp} already exist");

            ApiNamespaces[nsp] = new ROApiDescription
            {
                InstanceFields = apiList.InstanceFields?.ToList(),
                StaticFields = apiList.StaticFields?.ToList()
            };
        }

        public IEnumerable<string> GetHelp()
        {
            return RootApi.Values.Select(x => x.ToString())
                .Concat(ApiNamespaces.SelectMany(x => new[] { $"{x.Key} module:" }
                            .Concat(
                                x.Value.StaticFields?.Select(y => x.Key + "." + y.ToString()) ?? new string[0]
                            ).Append("instance fields:")
                            .Concat(
                                x.Value.InstanceFields?.Select(y => y.ToString()) ?? new string[0]
                            )
                       )
                 );
        }

        public CpuApi()
        {
            RootApi = new CpuRoot().StaticFields.ToDictionary(x => x.Name, x => x as CpuApiFunc);
            AddNamespace("MathExt", new CpuMathExt());
            AddNamespace(CpuQuaternion.Name, new CpuQuaternion());
            AddNamespace(CpuVector3.Name, new CpuVector3());
            //AddNamespace("Object", new CpuObject());
        }

    }
}
