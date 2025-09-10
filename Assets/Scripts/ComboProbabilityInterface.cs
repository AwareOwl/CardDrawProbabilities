using DeckUtilities.Tests;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DeckUtilities
{
    /// <summary>
    /// An MonoBehaviour that can be use to configure tests from the Inspector window.
    /// </summary>
    public class ComboProbabilityInterface : MonoBehaviour
    {
        [SerializeField] uint _deckSize = 10;
        [SerializeField] CardGroup[] _cardGroups = new CardGroup[] { new CardGroup("GA", 4), new CardGroup("GB", 3), new CardGroup("GC", 2), new CardGroup("GD", 1) };
        [SerializeField] ComboGroup[] _comboGroups = new ComboGroup[] { new ComboGroup("CA", new uint[] { 1, 1, 0, 0 }), new ComboGroup("CB", new uint[] { 0, 0, 1, 1 }) };

        private void OnEnable()
        {
            CalculateComboProbabilities();
        }

        [ContextMenu ("CalculateProbabilites")]
        public void CalculateComboProbabilities()
        {
            var output = ComboProbabilityUtilities.CalculateComboProbabilities(_deckSize, _cardGroups, _comboGroups, ComboProbabilityUtilities.AlgorithmVariant.Optimized);
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var probabilitySet in output.probabilitySets)
            {
                stringBuilder.Append($"Set: {probabilitySet._setName.ToString()}{System.Environment.NewLine}");
                foreach (var probabilityByDraw in probabilitySet._probabilitiesByDraw)
                    stringBuilder.Append(probabilityByDraw.ToString() + System.Environment.NewLine);
            }
            Debug.Log (stringBuilder.ToString());
        }

#if UNITY_EDITOR
        [ContextMenu("CalculateAndSaveAsTestFile")]
        public void CalculateAndSaveAsTestFile()
        {
            var testData = ScriptableObject.CreateInstance<ComboProbabilityTestData>();
            var output = ComboProbabilityUtilities.CalculateComboProbabilities(_deckSize, _cardGroups, _comboGroups);

            // Creating deep copies
            CardGroup[] cardGroupsCopy = new CardGroup[_cardGroups.Length];
            for (int i = 0; i < cardGroupsCopy.Length; i++)
                cardGroupsCopy[i] = new CardGroup(_cardGroups[i]);

            ComboGroup[] comboGroupsCopy = new ComboGroup[_comboGroups.Length];
            for (int i = 0; i < comboGroupsCopy.Length; i++)
                comboGroupsCopy[i] = new ComboGroup(_comboGroups[i]);

            testData.SetUp(_deckSize, cardGroupsCopy, comboGroupsCopy, output.probabilitySets);

            var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/ScriptableObjects/Tests/Test.asset");
            AssetDatabase.CreateAsset(testData, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}