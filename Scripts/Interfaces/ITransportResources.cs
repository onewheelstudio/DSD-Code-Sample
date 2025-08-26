using HexGame.Resources;
using System.Collections.Generic;

public interface ITransportResources
{
    HashSet<ResourceType> GetAllowedResources();
    void AddAllowedResource(ResourceType resource);
    void RemoveAllowedResource(ResourceType resource);
}
