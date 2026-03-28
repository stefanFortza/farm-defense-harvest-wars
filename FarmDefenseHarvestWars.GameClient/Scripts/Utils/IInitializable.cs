namespace FarmDefenseHarvestWars.GameClient.Scripts.Utils;

public interface IInitializable<T>
{
    // <summary>
    /// Initializes the object with the provided data. It is called before the object is ready.
    /// </summary>
    void Initialize(T data);
    bool IsInitialized { get; }
}