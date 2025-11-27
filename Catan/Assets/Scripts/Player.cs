using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private NetworkVariable<byte> _wood = new(0);
    private NetworkVariable<byte> _stone = new(0);
    private NetworkVariable<byte> _wheat = new(0);
    private NetworkVariable<byte> _brick = new(0);
    private NetworkVariable<byte> _sheep = new(0);
}
