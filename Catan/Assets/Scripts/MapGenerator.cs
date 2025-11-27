using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

public enum Tile
{
    Grass,
    Stone,
    Forest,
    Brick,
    Field,
    Desert,
}

public class MapGenerator : MonoBehaviour
{
    private static readonly (int q, int r, int s)[] Directions =
    {
        (-1, 0,1), (0,-1,1), (1,-1,0), (1,0,-1),(0,1,-1), (-1,1,0)
    };

    [SerializeField] private GameObject tilePrefab;

    [SerializeField] private Transform tileParent;
    [SerializeField] private Transform corners;
    [SerializeField] private Transform streets;

    [SerializeField] private float tileWidth = 20f;
    [SerializeField] private float tileHeight = 17.32051f;

    public void InitializeMap()
    {
        var tiles = new[]
        {
            Tile.Grass,
            Tile.Grass,
            Tile.Grass,
            Tile.Grass,
            Tile.Forest,
            Tile.Forest,
            Tile.Forest,
            Tile.Forest,
            Tile.Stone,
            Tile.Stone,
            Tile.Stone,
            Tile.Brick,
            Tile.Brick,
            Tile.Brick,
            Tile.Field,
            Tile.Field,
            Tile.Field,
            Tile.Field,
            Tile.Desert,
        };
        Shuffle(tiles);
        GenerateMap(tiles);
    }
    
    private static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = new Random().Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
    
    private void GenerateMap(Tile[] tiles)
    {
        var positions = GenerateTilePositions(tiles.Length);
        for (var i = 0; i < tiles.Length; i++)
        {
            var tileObject = Instantiate(tilePrefab, positions[i], Quaternion.identity, tileParent);
            tileObject.GetComponent<NetworkObject>().Spawn();
            var tile = tileObject.GetComponent<MapTile>();
            tile.SetType(tiles[i]);
            if (new Random().Next(0, 2) == 1)
                tile.Discover();
        }
    }

    private List<Vector3> GenerateTilePositions(int count)
    {
        var positions = new List<Vector3>();

        var currentPos = new Vector3Int(0, -1, -1);
        
        positions.Add(Vector3.zero);
        count--;

        int sideCounter = 1;

        while (count > 0)
        {
            currentPos += new Vector3Int(0, 1, -1);
            
            for (var side = 0; side < 6; side++)
            {
                var (dq, dr, ds) = Directions[side];
                for (var i = 0; i < sideCounter; i++)
                {
                    count--;
                    if (count < 0)
                        return positions;
                    currentPos += new Vector3Int(dq, dr, ds);
                    positions.Add(AxialToPosition(currentPos.x, currentPos.y, currentPos.z));
                }
            }
            sideCounter++;
        }
        
        return positions;
    }

    private Vector3 AxialToPosition(int q, int r, int s)
    {
        int ySum = r - s;
        float y = -ySum * 0.5f * tileHeight;
        float x = (q * (0.75f * tileWidth));
        return new Vector3(x, 0f, y);
    }

    private void OnDrawGizmos()
    {
        for (var i = 0; i < corners.childCount; i++)
        {
            Gizmos.DrawSphere(corners.GetChild(i).position, 1f);
        }

        Gizmos.color = Color.red;
        for (var i = 0; i < streets.childCount; i++)
        {
            var street = streets.GetChild(i);
            Gizmos.DrawLine(street.position + street.forward * 2, street.position - street.forward * 2f);
        }
    }
}
