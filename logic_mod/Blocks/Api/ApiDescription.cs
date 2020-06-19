using System.Collections.Generic;

namespace Logic.Blocks.Api
{
    public abstract class ApiDescription
    {
        public abstract List<CpuApiProperty> StaticFields { get; }
        public abstract List<CpuApiProperty> InstanceFields { get; }
    }
}
