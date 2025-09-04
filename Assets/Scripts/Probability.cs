using UnityEngine;

namespace DeckUtilities
{
    [System.Serializable]
    public struct Probability
    {
        public ulong _successfulCombinations;
        public ulong _totalCombinations;

        public Probability(ulong successfulCombinations, ulong totalCombinations)
        {
            this._successfulCombinations = successfulCombinations;
            this._totalCombinations = totalCombinations;
        }

        public override string ToString() => $"{_successfulCombinations}/{_totalCombinations} ({100f * _successfulCombinations / _totalCombinations:n3})";
    }
}
