using UI.DevelopmentCards;
using UnityEngine;

namespace Misc
{
    public static class RandomDevelopmentCard
    {
        private const float VictoryPointChance = 0.2f;
        private const float KnightChance = 0.48f;
        private const float RoadBuildingChance = 0.08f;
        private const float YearOfPlentyChance = 0.08f;
        private const float MonopolyChance = 0.08f;
        //private const float HangedKnightsChance = 0.08f;
        
        /// <summary>
        /// Returns a random development card with the odds defined by the const values in this class
        /// </summary>
        /// <returns>DevelopmentCard.Type</returns>
        public static DevelopmentCard.Type Next()
        {
            return DevelopmentCard.Type.Monopoly;
            float number = Random.Range(0f, 1f);
            number -= VictoryPointChance;
            if (number <= 0f)
                return DevelopmentCard.Type.VictoryPoint;
            number -= KnightChance;
            if (number <= 0f)
                return DevelopmentCard.Type.Knight;
            number -= RoadBuildingChance;
            if (number <= 0f)
                return DevelopmentCard.Type.RoadBuilding;
            number -= YearOfPlentyChance;
            if (number <= 0f)
                return DevelopmentCard.Type.YearOfPlenty;
            number -= MonopolyChance;
            if (number <= 0f)
                return DevelopmentCard.Type.Monopoly;
            return DevelopmentCard.Type.HangedKnights;
        }
    }
}