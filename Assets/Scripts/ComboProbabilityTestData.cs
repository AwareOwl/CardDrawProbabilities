using UnityEngine;

namespace DeckUtilities.Tests
{
    /// <summary>
    /// An input test for the algorithm that can used by Unity tests.
    /// </summary>
    [CreateAssetMenu(fileName = "ComboProbabilityTestData", menuName = "Scriptable Objects/ComboProbabilityTestData")]
    public class ComboProbabilityTestData : ScriptableObject
    {
        [SerializeField] uint _deckSize = 10;
        [SerializeField] CardGroup[] _cardGroups = new CardGroup[] { new CardGroup("GA", 4), new CardGroup("GB", 3), new CardGroup("GC", 2), new CardGroup("GD", 1) };
        [SerializeField] ComboGroup[] _comboGroups = new ComboGroup[] { new ComboGroup("CA", new uint[] { 1, 1, 0, 0 }), new ComboGroup("CB", new uint[] { 0, 0, 1, 1 }) };

        [SerializeField] ProbabilitySet[] _probabilities;
        
        public void SetUp(uint deckSize,CardGroup[] cardGroups, ComboGroup[] comboGroups, ProbabilitySet[] probabilities)
        {
            _deckSize = deckSize;
            _cardGroups = cardGroups;
            _comboGroups = comboGroups;
            _probabilities = probabilities;
        }

        public bool Validate()
        {
            var probabilitySets = ComboProbabilityUtilities.CalculateComboProbabilities(_deckSize, _cardGroups, _comboGroups).probabilitySets;
            if (probabilitySets.Length != _probabilities.Length)
            {
                Debug.Log($"[{name}] First dimension of arrays doesn't match ({probabilitySets.Length} != {_probabilities.Length})");
                return false;
            }
            for (int i = 0; i < probabilitySets.Length; i++)
            {
                if (probabilitySets[i]._probabilitiesByDraw.Length != _probabilities[i]._probabilitiesByDraw.Length)
                {
                    Debug.Log($"[{name}] Second dimension of arrays doesn't match ({probabilitySets[i]._probabilitiesByDraw.Length} != {_probabilities[i]._probabilitiesByDraw.Length})");
                    return false;
                }
                for (int j = 0; j < probabilitySets[i]._probabilitiesByDraw.Length; j++)
                {
                    if (probabilitySets[i]._probabilitiesByDraw[j]._successfulCombinations != _probabilities[i]._probabilitiesByDraw[j]._successfulCombinations)
                    {
                        Debug.Log($"[{name}] The number of successful sequences for i = {i} and j = {j} doesn't match ({probabilitySets[i]._probabilitiesByDraw[j]._successfulCombinations} != {_probabilities[i]._probabilitiesByDraw[j]._successfulCombinations})");
                        return false;
                    }
                    if (probabilitySets[i]._probabilitiesByDraw[j]._totalCombinations != _probabilities[i]._probabilitiesByDraw[j]._totalCombinations)
                    {
                        Debug.Log($"[{name}] The number of total sequences for i = {i} and j = {j} doesn't match ({probabilitySets[i]._probabilitiesByDraw[j]._totalCombinations} != {_probabilities[i]._probabilitiesByDraw[j]._totalCombinations})");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}