using TMPro;
using UI;
using Unity.Netcode;
using UnityEngine;

public class MapTile : NetworkBehaviour
{
    private static readonly Color HighOddsTileColor = Color.red;
    public Tile TileType => (Tile)_tileType.Value;
    public int Number => _number.Value;
    
    private readonly NetworkVariable<bool> _discovered = new();
    private readonly NetworkVariable<int> _tileType = new(-1);
    private readonly NetworkVariable<int> _number = new(-1);
    [SerializeField] private ParticleSystem fog;
    [SerializeField] private Transform tileParent;
    [SerializeField] private GameObject hiddenTile;
    [SerializeField] private GameObject numberTextPrefab;
    
    private MapIcon _mapIcon;
    private GameObject _numberText;
    private Transform _banditPosition;

    public override void OnNetworkSpawn()
    {
        CreateNumberComponent();
        _discovered.OnValueChanged += DiscoverStatusChanged;
        _number.OnValueChanged += NumberValueChanged;
        _tileType.OnValueChanged += (_, _) => UpdateTile();
        if (_discovered.Value == false) fog.Play();
        hiddenTile.SetActive(_discovered.Value == false);
        UpdateTile();
    }

    private void UpdateTile()
    {
        if (_tileType.Value < 0) return;
        hiddenTile.SetActive(!_discovered.Value);
        tileParent.gameObject.SetActive(_discovered.Value);
        _banditPosition = tileParent.GetChild(_tileType.Value).GetComponentInChildren<BanditPositionMarker>().transform;
        for (var i = 0; i < tileParent.childCount; i++)
        {
            tileParent.GetChild(i).gameObject.SetActive(_tileType.Value == i);
        }
    }

    public void SetType(Tile tile)
    {
        _tileType.Value = (int)tile;
    }

    public void SetNumber(int number)
    {
        _number.Value = number;
    }

    public void Discover()
    {
        _discovered.Value = true;
    }

    private void NumberValueChanged(int previous, int current)
    {
        _numberText.GetComponent<TextMeshProUGUI>().text = current.ToString();
        if (current is 6 or 8)
            _numberText.GetComponent<TextMeshProUGUI>().color = HighOddsTileColor;
    }

    private void CreateNumberComponent()
    {
        _numberText = Instantiate(numberTextPrefab);
        _numberText.GetComponent<TextMeshProUGUI>().text = _number.Value.ToString();
        if (_number.Value is 6 or 8)
            _numberText.GetComponent<TextMeshProUGUI>().color = HighOddsTileColor;
        _numberText.SetActive(false);
    }

    private void DiscoverStatusChanged(bool oldValue, bool newValue)
    {
        hiddenTile.SetActive(!newValue);
        tileParent.gameObject.SetActive(newValue);
        if (newValue)
        {
            fog.Stop();
            if (_tileType.Value != (int)Tile.Desert)
            {
                _mapIcon = MapIconManager.AddIcon(transform, IconType.Tile, Color.white);
                _numberText.transform.SetParent(_mapIcon.transform, false);
                _numberText.SetActive(true);   
            }
        }
        else
        {
            fog.Play();   
        }
    }
}