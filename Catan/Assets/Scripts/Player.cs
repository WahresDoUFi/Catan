using System;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    public enum ResourceType
    {
        Wood,
        Stone,
        Wheat,
        Brick,
        Sheep
    }
    public event Action ResourcesUpdated;
    public int ResourceCount => _wood.Value + _stone.Value + _wheat.Value + _brick.Value + _sheep.Value;
    public byte Wood => _wood.Value;
    public byte Stone => _stone.Value;
    public byte Wheat => _wheat.Value;
    public byte Brick => _brick.Value;
    public byte Sheep => _sheep.Value;
    
    private readonly NetworkVariable<byte> _wood = new();
    private readonly NetworkVariable<byte> _stone = new();
    private readonly NetworkVariable<byte> _wheat = new();
    private readonly NetworkVariable<byte> _brick = new();
    private readonly NetworkVariable<byte> _sheep = new();

    public override void OnNetworkSpawn()
    {
        _wood.OnValueChanged += ResourceCountChanged;
        _stone.OnValueChanged += ResourceCountChanged;
        _wheat.OnValueChanged += ResourceCountChanged;
        _brick.OnValueChanged += ResourceCountChanged;
        _sheep.OnValueChanged += ResourceCountChanged;
        
        UpdateResources(ResourceType.Wheat, 3);
        UpdateResources(ResourceType.Stone, 3);
    }

    public void UpdateResources(ResourceType type, byte amount)
    {
        switch (type)
        {
            case ResourceType.Wood:
                _wood.Value += amount;
                break;
            case ResourceType.Stone:
                _stone.Value += amount;
                break;
            case ResourceType.Wheat:
                _wheat.Value += amount;
                break;
            case ResourceType.Brick:
                _brick.Value += amount;
                break;
            case ResourceType.Sheep:
                _sheep.Value += amount;
                break;
            default:
                return;
        }
    }

    private void ResourceCountChanged(byte previous, byte current)
    {
        ResourcesUpdated?.Invoke();
    }
}
