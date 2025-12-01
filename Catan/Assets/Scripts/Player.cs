using System;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    public static Player LocalPlayer { get; private set; }
    public event Action ResourcesUpdated;
    public int ResourceCount => _wood.Value + _stone.Value + _wheat.Value + _brick.Value + _sheep.Value;
    public byte Wood => _wood.Value;
    public byte Stone => _stone.Value;
    public byte Wheat => _wheat.Value;
    public byte Brick => _brick.Value;
    public byte Sheep => _sheep.Value;
    public byte VictoryPoints => _victoryPoints.Value;
    
    private readonly NetworkVariable<byte> _wood = new();
    private readonly NetworkVariable<byte> _stone = new();
    private readonly NetworkVariable<byte> _wheat = new();
    private readonly NetworkVariable<byte> _brick = new();
    private readonly NetworkVariable<byte> _sheep = new();
    private readonly NetworkVariable<byte> _victoryPoints = new();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            LocalPlayer = this;
        
        _wood.OnValueChanged += ResourceCountChanged;
        _stone.OnValueChanged += ResourceCountChanged;
        _wheat.OnValueChanged += ResourceCountChanged;
        _brick.OnValueChanged += ResourceCountChanged;
        _sheep.OnValueChanged += ResourceCountChanged;
        
        UpdateResources(Tile.Field, 3);
        UpdateResources(Tile.Stone, 3);
    }

    public static Player GetPlayerById(ulong clientId)
    {
        return NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
    }

    public void AddVictoryPoints(byte points)
    {
        _victoryPoints.Value += points;
    }
    public void RemoveVictoryPoints(byte points)
    {
        _victoryPoints.Value -= points;
    }

    public void UpdateResources(Tile type, byte amount)
    {
        switch (type)
        {
            case Tile.Forest:
                _wood.Value += amount;
                break;
            case Tile.Stone:
                _stone.Value += amount;
                break;
            case Tile.Field:
                _wheat.Value += amount;
                break;
            case Tile.Brick:
                _brick.Value += amount;
                break;
            case Tile.Grass:
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
