using TMPro;
using UI;
using Unity.Netcode;
using UnityEngine;

public class MapTile : NetworkBehaviour
{
    private static Color _highOddsTileColor = Color.red;
    public Tile TileType => (Tile)_tileType.Value;
    private readonly NetworkVariable<bool> _discovered = new();
    private readonly NetworkVariable<int> _tileType = new(-1);
    private readonly NetworkVariable<int> _number = new(-1);
    [SerializeField] private ParticleSystem fog;
    [SerializeField] private Transform tileParent;
    [SerializeField] private GameObject hiddenTile;
    [SerializeField] private GameObject numberTextPrefab;
    
    private MapIcon _mapIcon;
    private GameObject _numberText;

    public override void OnNetworkSpawn()
    {
        _discovered.OnValueChanged += DiscoverStatusChanged;
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
        _numberText = Instantiate(numberTextPrefab);
        _numberText.GetComponent<TextMeshProUGUI>().text = number.ToString();
        if (number is 6 or 8)
            _numberText.GetComponent<TextMeshProUGUI>().color = _highOddsTileColor;
        _numberText.SetActive(false);
    }

    public void Discover()
    {
        _discovered.Value = true;
    }

    private void DiscoverStatusChanged(bool oldValue, bool newValue)
    {
        hiddenTile.SetActive(!newValue);
        tileParent.gameObject.SetActive(newValue);
        if (!newValue)
        {
            fog.Play();   
        }
        else
        {
            fog.Stop();
            if (_numberText)
            {
                _mapIcon = MapIconManager.AddIcon(transform, IconType.Tile, Color.white);
                _numberText.transform.SetParent(_mapIcon.transform, false);
                _numberText.SetActive(true);   
            }
        }
    }
}