using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using User;

namespace GamePlay
{
    public static class VictoryPoints
    {
        public static int CalculateVictoryPoints(ulong clientId)
        {
            var victoryPoints = 0;
            victoryPoints += CalculateForBuildings(clientId) + Player.GetPlayerById(clientId).AdditionalVictoryPoints;
            if (HasLongestStreet(clientId)) victoryPoints += 2;
            if (HasMostKnightCards(clientId)) victoryPoints += 2;

            return victoryPoints;
        }

        public static int GetLongestStreetForPlayer(ulong clientId)
        {
            var maxLength = 0;
            var allStreets = Street.AllStreets.Where(s => s.Owner == clientId).ToList();
            foreach (var street in allStreets)
            {
                var length = CalculateLongestStreet(street, clientId, new HashSet<Street>(), new HashSet<Settlement>());
                maxLength = Math.Max(maxLength, length);
            }

            return maxLength;
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

        private static bool HasLongestStreet(ulong clientId)
        {
            var street = Player.GetPlayerById(clientId).LongestStreet;
            if (street < 5) return false;
            foreach (var playerId in NetworkManager.Singleton.ConnectedClients.Keys)
            {
                if (playerId == clientId) continue;
                if (Player.GetPlayerById(playerId).LongestStreet >= street) return false;
            }

            return true;
        }

        private static bool HasMostKnightCards(ulong clientId)
        {
            byte cards = Player.GetPlayerById(clientId).KnightCardsPlayed;
            if (cards < 3) return false;
            foreach (var playerId in GameManager.Instance.GetPlayerIds())
            {
                if (playerId == clientId) continue;
                if (Player.GetPlayerById(playerId).KnightCardsPlayed >= cards) return false;
            }

            return true;
        }

        private static int CalculateLongestStreet(Street currentStreet, ulong clientId, HashSet<Street> visitedStreets,
            HashSet<Settlement> visitedSettlements)
        {
            visitedStreets.Add(currentStreet);
            var maxPath = 0;

            foreach (var next in currentStreet.connectedStreets)
            {
                // if street was already visited or is broken up by other players street
                if (next.Owner != clientId || visitedStreets.Contains(next)) continue;
                var commonSettlement = GetCommonSettlement(currentStreet, next);
                // if street is broken up by enemy settlement
                if (commonSettlement != null && commonSettlement.IsOccupied && commonSettlement.Owner != clientId)
                    continue;
                // if settlement was already visited or does not exist
                if (commonSettlement == null || !visitedSettlements.Add(commonSettlement))
                    continue;

                var pathLength = CalculateLongestStreet(next, clientId, visitedStreets, visitedSettlements);
                maxPath = Math.Max(maxPath, pathLength);

                visitedSettlements.Remove(commonSettlement);
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
}