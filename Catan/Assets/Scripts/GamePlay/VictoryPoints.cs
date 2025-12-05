using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

public static class VictoryPoints
{
    public static int CalculateVictoryPoints(ulong clientId)
    {
        var victoryPoints = 0;
        victoryPoints += CalculateForBuildings(clientId);
        if (HasLongestStreet(clientId))
        {
            victoryPoints += 2;
        }

        return victoryPoints;
    }

    private static int CalculateForBuildings(ulong clientId)
    {
        int points = 0;
        foreach (var settlement in Settlement.AllSettlements)
        {
            if (settlement.Owner == clientId)
            {
                points += settlement.Level;
            }
        }

        return points;
    }

    private static bool HasLongestStreet(ulong playerId)
    {
        var longestRoadForPlayer = CalculateLongestStreet(playerId);
        if (longestRoadForPlayer < 5) return false;
        foreach (var clientId in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            if (clientId == playerId) continue;
            if (CalculateLongestStreet(clientId) >= longestRoadForPlayer)
                return false;
        }

        return true;
    }

    private static int CalculateLongestStreet(ulong clientId)
    {
        var maxLength = 0;
        var allStreets = Street.AllStreets.Where(s => s.Owner == clientId).ToList();
        foreach (var street in allStreets)
        {
            var length = Algo(street, clientId, new HashSet<Street>());
            maxLength = Math.Max(maxLength, length);
        }

        return maxLength;
    }

    private static int Algo(Street currentStreet, ulong clientId, HashSet<Street> visitedStreets)
    {
        visitedStreets.Add(currentStreet);
        var maxPath = 0;

        foreach (var next in currentStreet.connectedStreets)
        {
            if (next.Owner != clientId || visitedStreets.Contains(next)) continue;
            var commonSettlement = GetCommonSettlement(currentStreet, next);
            if (commonSettlement != null && commonSettlement.IsOccupied && commonSettlement.Owner != clientId)
                continue;
            var pathLength = Algo(next, clientId, visitedStreets);
            maxPath = Math.Max(maxPath, pathLength);
        }

        visitedStreets.Remove(currentStreet);
        return maxPath + 1;
    }
    
    private static Settlement GetCommonSettlement(Street street1, Street street2)
    {
        foreach (var settlement in street1.settlements)
        {
            if (street2.settlements.Contains(settlement))
            {
                return settlement;
            }
        }
        return null;
    }
}