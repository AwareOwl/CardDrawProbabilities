using System.Xml.Linq;
using UnityEngine;

namespace DeckUtilities
{
    [System.Serializable]
    public class ProbabilitySet
    {
        public string _setName;
        public Probability [] _probabilitiesByDraw;

        public ProbabilitySet(ComboGroup comboGroup)
        {
            _setName = comboGroup._groupName;
            _probabilitiesByDraw = new Probability[comboGroup._targetDraw];
        }

        public bool Compare (ProbabilitySet otherProbabilitySet)
        {
            if (_probabilitiesByDraw.Length != otherProbabilitySet._probabilitiesByDraw.Length)
            {
                Debug.Log($"Second dimension of arrays doesn't match ({_probabilitiesByDraw.Length} != {otherProbabilitySet._probabilitiesByDraw.Length})");
                return false;
            }
            for (int j = 0; j < _probabilitiesByDraw.Length; j++)
            {
                if (_probabilitiesByDraw[j]._successfulCombinations != otherProbabilitySet._probabilitiesByDraw[j]._successfulCombinations)
                {
                    Debug.Log($"The number of successful sequences for draw = {j} doesn't match ({_probabilitiesByDraw[j]._successfulCombinations} != {otherProbabilitySet._probabilitiesByDraw[j]._successfulCombinations})");
                    return false;
                }
                if (_probabilitiesByDraw[j]._totalCombinations != otherProbabilitySet._probabilitiesByDraw[j]._totalCombinations)
                {
                    Debug.Log($"The number of total sequences for draw = {j} doesn't match ({_probabilitiesByDraw[j]._totalCombinations} != {otherProbabilitySet._probabilitiesByDraw[j]._totalCombinations})");
                    return false;
                }
            }
            return true;
        }
    }
}
