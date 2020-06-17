using Logic.Script;
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
        Dictionary<string, CpuApiFunc> RootApi;
        Dictionary<string, Dictionary<string, CpuApiFunc>> ApiNamespaces = new Dictionary<string, Dictionary<string, CpuApiFunc>>();

        public override string Name => "CpuApi";

        public void Attach(CpuBlock block, FuncCtx ctx)
        {
            foreach (var api in RootApi)
                block.Interp.AddExtFunc(ctx, api.Key, api.Value.ImplementationFactory(block), api.Value.Sync);

            foreach (var kp in ApiNamespaces)
                block.Interp.AddExtVariable(ctx, kp.Key, kp.Value.ToDictionary(x => x.Key, x => (object)block.Interp.CreateFunc(ctx, x.Value.Name, x.Value.ImplementationFactory(block), x.Value.Sync)));
        }

        public void AddNamespace(string nsp, ApiList apiList)
        {
            if (string.IsNullOrEmpty(nsp))
                throw new Exception("Namespace cannot be empty");

            var apiDict = apiList.Api.ToDictionary(x => x.Name);
            if (!ApiNamespaces.ContainsKey(nsp))
            {
                ApiNamespaces[nsp] = new Dictionary<string, CpuApiFunc>();
            }
            else
            {
                var dupKey = ApiNamespaces[nsp].Keys.Where(x => apiDict.ContainsKey(x)).FirstOrDefault();
                if (dupKey != null)
                    throw new Exception($"Failed adding functions to {nsp}: duplicate name {dupKey} found!");
            }

            foreach (var api in apiDict)
                ApiNamespaces[nsp].Add(api.Key, api.Value);
        }

        public IEnumerable<string> GetHelp()
        {
            return RootApi.Values.Select(x => x.ToString())
                .Concat(ApiNamespaces.SelectMany(x => new[] { $"{x.Key} module:" }.Concat(x.Value.Values.Select(y => x.Key+"."+y.ToString()))));
        }

        public CpuApi()
        {
            RootApi = new CpuRoot().Api.ToDictionary(x => x.Name);
            AddNamespace("Math", new CpuMath());
            AddNamespace("Object", new CpuObject());
            AddNamespace("Quaternion", new CpuQuat());
        }

    }
}
