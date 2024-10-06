using System.Collections.Generic;

namespace HexGame.Resources
{
    public interface IUseResource
    {
        bool TryUseAllResources(List<ResourceAmount> resourceList);
    }
}
