namespace FarmDefenseHarvestWars.GameClient.Scripts.Utils;

public interface IInitializable<T>
{
    void Initialize(T data);
    bool IsInitialized { get; }
}