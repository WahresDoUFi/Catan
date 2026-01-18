using System;
using Unity.Netcode;

public static class DiceRoll
{ 
    public static (int first, int second) GetResult(int seed)
    {
#if UNITY_EDITOR
        //  debug result, remove or update as needed
        return (6, 1);
#endif
        var random = new Random(seed);
        var die = random.Next(1, 7);
        var secondDie = random.Next(1, 7);

        return (die, secondDie);
    }
}