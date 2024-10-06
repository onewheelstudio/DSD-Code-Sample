using HexGame.Resources;
using System.Collections.Generic;

public interface IHaveReceipes
{
    List<ResourceProduction> GetReceipes();
    void SetReceipe(int receipe);
    int GetCurrentRecipe();
    float GetEfficiency();
    float GetTimeToProduce();

    float GetStartTime();
}
