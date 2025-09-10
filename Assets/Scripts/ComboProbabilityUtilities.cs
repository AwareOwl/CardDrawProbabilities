using System;
using UnityEngine;

namespace DeckUtilities
{

    public class ComboProbabilityUtilities
    {

        /// <summary>
        /// Algorithm variant. Each variant returns an extra data array: 
        /// Full table - it returns the table of possible sequences of each draw combination.The first dimension represents the number of the drawn cards. Other N dimensions represent the N different card groups. It's great for making charts, as it contains every possible data;
        /// Cropped table - similar to the full table, but the dimensions represeting card groups have bounds equal to the max expected value in corresponding card groups.For example, if the first card group contains 10 cards, but player only needs 2 of them, then the corresponding array will have legnth 3. The values not fitting the bounds are merged with corresponding edge cells of the array, which can be interpreted as a sufix sum;
        /// Optimized - it returns a one-dimensional table, containing only the data from the last needed card draw. It's the most performant.
        /// </summary>
        public enum AlgorithmVariant
        {
            FullTable,
            CroppedTable,
            Optimized,
        }

        /// <summary>
        /// Calculates the number of sequences that fulfill combo criteria for each distinguish card draw combination.
        /// </summary>
        /// <param name="deckSize">The number of cards in deck.</param>
        /// <param name="cardGroups">Card quantitites for each relevant card type in the deck.</param>
        /// <param name="comboGroups">Combos that require a certain number of cards from each group by a certain turn to fulfill the criteria.</param>
        /// <param name="algorithmVariant">Algorithm variant. Each variant returns an extra data array: 
        /// Full table - it returns the table of possible sequences of each draw combination.The first dimension represents the number of the drawn cards. Other N dimensions represent the N different card groups. It's great for making charts, as it contains every possible data;
        /// Cropped table - similar to the full table, but the dimensions represeting card groups have bounds equal to the max expected value in corresponding card groups.For example, if the first card group contains 10 cards, but player only needs 2 of them, then the corresponding array will have legnth 3. The values not fitting the bounds are merged with corresponding edge cells of the array, which can be interpreted as a sufix sum;
        /// Optimized - it returns a one-dimensional table, containing only the data from the last needed card draw. It's the most performant.</param>
        /// <returns>An array of probability sets, where each probability set corresponds one card combination (e.g., 1 card B and 1 card C by the 9th draw). In each probability set, you can find alias of the card combination, and probabilities of it happening during each turn. Keep in mind that probabilities use AND logic - all combinations that do not fulfill previously checked restriction will be cut out.</returns>
        public static (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilities(uint deckSize, CardGroup[] cardGroups, ComboGroup[] comboGroups, AlgorithmVariant algorithmVariant = AlgorithmVariant.Optimized)
        {
            // Initial validation
            if (comboGroups.Length > 4)
            {
                throw new Exception("This algorithm doesn't support more than 4 combo groups.");
            }
            switch (algorithmVariant)
            {
                case AlgorithmVariant.FullTable:
                    return CalculateComboProbabilitiesFullTable();
                case AlgorithmVariant.CroppedTable:
                    return CalculateComboProbabilitiesCroppedTable();
                case AlgorithmVariant.Optimized:
                    return CalculateComboProbabilitiesOptimized();
                default:
                    return CalculateComboProbabilitiesOptimized();
            }

            (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesFullTable()
            {
                switch (cardGroups.Length)
                {
                    case 0:
                        throw new Exception ("The cardGroups array is empty.");
                    case 1:
                        return CalculateComboProbabilitiesFullTable1D();
                    case 2:
                        return CalculateComboProbabilitiesFullTable2D();
                    case 3:
                        return CalculateComboProbabilitiesFullTable3D();
                    case 4:
                        return CalculateComboProbabilitiesFullTable4D();
                    default:
                        throw new Exception("This algorithm doesn't support more than 4 combo groups.");
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesFullTable1D()
                {
                    uint required = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required = Math.Max(required, comboGroup._requiredCount[0]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups[0]._groupSize;

                    if (all0 > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = all0;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = all0 + 1;

                    ulong[,] table = new ulong[totalCardDraws + 1, arrayLength0];
                    table[0, 0] = 1;
                    uint otherCards = deckSize - all0;
                    ulong totalCombinations = 1;

                    uint drawn0;

                    uint remaining0;

                    uint remainingCards = deckSize;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1, previouslyDrawn = 0; drawn <= totalCardDraws; drawn++, previouslyDrawn++, remainingCards--)
                    {
                        remaining0 = all0;
                        for (drawn0 = 0; drawn0 < arrayLength0; drawn0++, remaining0--)
                        {
                            if (otherCards < remainingCards - remaining0)
                                continue;
                            ulong previousSequences = table[previouslyDrawn, drawn0];
                            if (previousSequences == 0)
                                continue;
                            table[drawn, drawn0] += previousSequences * (remainingCards - remaining0);
                            if (drawn0 < all0)
                                table[drawn, drawn0 + 1] += previousSequences * remaining0;
                        }

                        totalCombinations *= remainingCards;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount.Length > 0 ? comboGroup._requiredCount[0] : 0;
                            uint comboRequired1 = comboGroup._requiredCount.Length > 1 ? comboGroup._requiredCount[1] : 0;

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    successfulCombinations += table[drawn, drawn0];
                                }
                            }
                            else
                            {
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    if (drawn0 >= comboRequired0)
                                    {
                                        successfulCombinations += table[drawn, drawn0];
                                    }
                                    else if (comboGroup._targetDraw == drawn)
                                    {
                                        table[drawn, drawn0] = 0;
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesFullTable2D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups[0]._groupSize;
                    uint all1 = cardGroups[1]._groupSize;
                    uint allSum = all0 + all1;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = all0 + 1;
                    uint arrayLength1 = all1 + 1;

                    ulong[,,] table = new ulong[totalCardDraws + 1, arrayLength0, arrayLength1];
                    table[0, 0, 0] = 1;
                    uint otherCards = deckSize - allSum;
                    ulong totalCombinations = 1;

                    uint drawn0;
                    uint drawn1;

                    uint remaining0;
                    uint remaining1;
                    uint remainingSum;

                    uint remainingCards = deckSize;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1, previouslyDrawn = 0; drawn <= totalCardDraws; drawn++, previouslyDrawn++, remainingCards--)
                    {
                        remainingSum = allSum;
                        remaining0 = all0;
                        for (drawn0 = 0; drawn0 < arrayLength0; drawn0++, remaining0--, remainingSum--)
                        {
                            remaining1 = all1;
                            for (drawn1 = 0; drawn1 < arrayLength1; drawn1++, remaining1--, remainingSum--)
                            {
                                if (otherCards < remainingCards - remainingSum)
                                    continue;
                                ulong previousSequences = table[previouslyDrawn, drawn0, drawn1];
                                if (previousSequences == 0)
                                    continue;
                                table[drawn, drawn0, drawn1] += previousSequences * (remainingCards - remainingSum);
                                if (drawn0 < all0)
                                    table[drawn, drawn0 + 1, drawn1] += previousSequences * remaining0;
                                if (drawn1 < all1)
                                    table[drawn, drawn0, drawn1 + 1] += previousSequences * remaining1;
                            }
                            remainingSum += arrayLength1;
                        }

                        totalCombinations *= remainingCards;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount.Length > 0 ? comboGroup._requiredCount[0] : 0;
                            uint comboRequired1 = comboGroup._requiredCount.Length > 1 ? comboGroup._requiredCount[1] : 0;

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                        successfulCombinations += table[drawn, drawn0, drawn1];
                                    }
                                }
                            }
                            else
                            {
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        if (check0 && (drawn1 >= comboRequired1))
                                        {
                                            successfulCombinations += table[drawn, drawn0, drawn1];
                                        }
                                        else if (comboGroup._targetDraw == drawn)
                                        {
                                            table[drawn, drawn0, drawn1] = 0;
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesFullTable3D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint required2 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        if (comboGroup._requiredCount.Length > 2)
                            required2 = Math.Max(required2, comboGroup._requiredCount[2]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups[0]._groupSize;
                    uint all1 = cardGroups[1]._groupSize;
                    uint all2 = cardGroups[2]._groupSize;
                    uint allSum = all0 + all1 + all2;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = all0 + 1;
                    uint arrayLength1 = all1 + 1;
                    uint arrayLength2 = all2 + 1;

                    ulong[,,,] table = new ulong[totalCardDraws + 1, arrayLength0, arrayLength1, arrayLength2];
                    table[0, 0, 0, 0] = 1;
                    uint otherCards = deckSize - allSum;
                    ulong totalCombinations = 1;

                    uint drawn0;
                    uint drawn1;
                    uint drawn2;

                    uint remaining0;
                    uint remaining1;
                    uint remaining2;
                    uint remainingSum;

                    uint remainingCards = deckSize;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1, previouslyDrawn = 0; drawn <= totalCardDraws; drawn++, previouslyDrawn++, remainingCards--)
                    {
                        remainingSum = allSum;
                        remaining0 = all0;
                        for (drawn0 = 0; drawn0 < arrayLength0; drawn0++, remaining0--, remainingSum--)
                        {
                            remaining1 = all1;
                            for (drawn1 = 0; drawn1 < arrayLength1; drawn1++, remaining1--, remainingSum--)
                            {
                                remaining2 = all2;
                                for (drawn2 = 0; drawn2 < arrayLength2; drawn2++, remaining2--, remainingSum--)
                                {
                                    if (otherCards < remainingCards - remainingSum)
                                        continue;
                                    ulong previousSequences = table[previouslyDrawn, drawn0, drawn1, drawn2];
                                    if (previousSequences == 0)
                                        continue;
                                    table[drawn, drawn0, drawn1, drawn2] += previousSequences * (remainingCards - remainingSum);
                                    if (drawn0 < all0)
                                        table[drawn, drawn0 + 1, drawn1, drawn2] += previousSequences * remaining0;
                                    if (drawn1 < all1)
                                        table[drawn, drawn0, drawn1 + 1, drawn2] += previousSequences * remaining1;
                                    if (drawn2 < all2)
                                        table[drawn, drawn0, drawn1, drawn2 + 1] += previousSequences * remaining2;
                                }
                                remainingSum += arrayLength2;
                            }
                            remainingSum += arrayLength1;
                        }

                        totalCombinations *= remainingCards;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount.Length > 0 ? comboGroup._requiredCount[0] : 0;
                            uint comboRequired1 = comboGroup._requiredCount.Length > 1 ? comboGroup._requiredCount[1] : 0;
                            uint comboRequired2 = comboGroup._requiredCount.Length > 2 ? comboGroup._requiredCount[2] : 0;

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                        for (drawn2 = comboRequired2; drawn2 < arrayLength2; drawn2++)
                                        {
                                            successfulCombinations += table[drawn, drawn0, drawn1, drawn2];
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        bool check1 = drawn1 >= comboRequired1;
                                        for (drawn2 = 0; drawn2 < arrayLength2; drawn2++)
                                        {
                                            if (check0 && check1 && (drawn2 >= comboRequired2))
                                            {
                                                successfulCombinations += table[drawn, drawn0, drawn1, drawn2];
                                            }
                                            else if (comboGroup._targetDraw == drawn)
                                            {
                                                table[drawn, drawn0, drawn1, drawn2] = 0;
                                            }
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesFullTable4D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint required2 = 0;
                    uint required3 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        if (comboGroup._requiredCount.Length > 2)
                            required2 = Math.Max(required2, comboGroup._requiredCount[2]);
                        if (comboGroup._requiredCount.Length > 3)
                            required3 = Math.Max(required3, comboGroup._requiredCount[3]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups[0]._groupSize;
                    uint all1 = cardGroups[1]._groupSize;
                    uint all2 = cardGroups[2]._groupSize;
                    uint all3 = cardGroups[3]._groupSize;
                    uint allSum = all0 + all1 + all2 + all3;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("-----Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = all0 + 1;
                    uint arrayLength1 = all1 + 1;
                    uint arrayLength2 = all2 + 1;
                    uint arrayLength3 = all3 + 1;

                    ulong[,,,,] table = new ulong[totalCardDraws + 1, arrayLength0, arrayLength1, arrayLength2, arrayLength3];
                    table[0, 0, 0, 0, 0] = 1;
                    uint otherCards = deckSize - allSum;
                    ulong totalCombinations = 1;

                    uint drawn0;
                    uint drawn1;
                    uint drawn2;
                    uint drawn3;

                    uint remaining0;
                    uint remaining1;
                    uint remaining2;
                    uint remaining3;
                    uint remainingSum;

                    uint remainingCards = deckSize;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1, previouslyDrawn = 0; drawn <= totalCardDraws; drawn++, previouslyDrawn++, remainingCards--)
                    {
                        remainingSum = allSum;
                        remaining0 = all0;
                        for (drawn0 = 0; drawn0 < arrayLength0; drawn0++, remaining0--, remainingSum--)
                        {
                            remaining1 = all1;
                            for (drawn1 = 0; drawn1 < arrayLength1; drawn1++, remaining1--, remainingSum--)
                            {
                                remaining2 = all2;
                                for (drawn2 = 0; drawn2 < arrayLength2; drawn2++, remaining2--, remainingSum--)
                                {
                                    remaining3 = all3;
                                    for (drawn3 = 0; drawn3 < arrayLength3; drawn3++, remaining3--, remainingSum--)
                                    {
                                        if (otherCards < remainingCards - remainingSum)
                                            continue;
                                        ulong previousSequences = table[previouslyDrawn, drawn0, drawn1, drawn2, drawn3];
                                        if (previousSequences == 0)
                                            continue;
                                        table[drawn, drawn0, drawn1, drawn2, drawn3] += previousSequences * (remainingCards - remainingSum);
                                        if (drawn0 < all0)
                                            table[drawn, drawn0 + 1, drawn1, drawn2, drawn3] += previousSequences * remaining0;
                                        if (drawn1 < all1)
                                            table[drawn, drawn0, drawn1 + 1, drawn2, drawn3] += previousSequences * remaining1;
                                        if (drawn2 < all2)
                                            table[drawn, drawn0, drawn1, drawn2 + 1, drawn3] += previousSequences * remaining2;
                                        if (drawn3 < all3)
                                            table[drawn, drawn0, drawn1, drawn2, drawn3 + 1] += previousSequences * remaining3;
                                    }
                                    remainingSum += arrayLength3;
                                }
                                remainingSum += arrayLength2;
                            }
                            remainingSum += arrayLength1;
                        }

                        totalCombinations *= remainingCards;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount.Length > 0 ? comboGroup._requiredCount[0] : 0;
                            uint comboRequired1 = comboGroup._requiredCount.Length > 1 ? comboGroup._requiredCount[1] : 0;
                            uint comboRequired2 = comboGroup._requiredCount.Length > 2 ? comboGroup._requiredCount[2] : 0;
                            uint comboRequired3 = comboGroup._requiredCount.Length > 3 ? comboGroup._requiredCount[3] : 0;

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                        for (drawn2 = comboRequired2; drawn2 < arrayLength2; drawn2++)
                                        {
                                            for (drawn3 = comboRequired3; drawn3 < arrayLength3; drawn3++)
                                            {
                                                successfulCombinations += table[drawn, drawn0, drawn1, drawn2, drawn3];
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        bool check1 = drawn1 >= comboRequired1;
                                        for (drawn2 = 0; drawn2 < arrayLength2; drawn2++)
                                        {
                                            bool check2 = drawn2 >= comboRequired2;
                                            for (drawn3 = 0; drawn3 < arrayLength3; drawn3++)
                                            {
                                                if (check0 && check1 && check2 && (drawn3 >= comboRequired3))
                                                {
                                                    successfulCombinations += table[drawn, drawn0, drawn1, drawn2, drawn3];
                                                }
                                                else if (comboGroup._targetDraw == drawn)
                                                {
                                                    table[drawn, drawn0, drawn1, drawn2, drawn3] = 0;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }
            }

            (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesCroppedTable()
            {
                switch (cardGroups.Length)
                {
                    case 0:
                        throw new Exception("The cardGroups array is empty.");
                    case 1:
                        return CalculateComboProbabilitiesCroppedTable1D();
                    case 2:
                        return CalculateComboProbabilitiesCroppedTable2D();
                    case 3:
                        return CalculateComboProbabilitiesCroppedTable3D();
                    case 4:
                        return CalculateComboProbabilitiesCroppedTable4D();
                    default:
                        throw new Exception("This algorithm doesn't support more than 4 combo groups.");
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesCroppedTable1D()
                {
                    uint required0 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups[0]._groupSize;

                    if (all0 > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = all0;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = required0 + 1;

                    ulong[,] table = new ulong[totalCardDraws + 1, arrayLength0];
                    table[0, 0] = 1;

                    uint drawn0;

                    uint remaining0;

                    ulong totalCombinations = 1;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1; drawn <= totalCardDraws; drawn++)
                    {
                        uint previouslyDrawn = drawn - 1;
                        uint cardsInDeck = deckSize - previouslyDrawn;

                        for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                        {
                            ulong previousSequences = table[previouslyDrawn, drawn0];
                            if (previousSequences == 0)
                                continue;

                            remaining0 = drawn0 < required0 ? all0 - drawn0 : 0;

                            if (cardsInDeck > remaining0)
                                table[drawn, drawn0] += previousSequences * (cardsInDeck - remaining0);
                            if (drawn0 < required0)
                                table[drawn, drawn0 + 1] += previousSequences * remaining0;
                        }

                        totalCombinations *= cardsInDeck;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount[0];

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    successfulCombinations += table[drawn, drawn0];
                                }
                            }
                            else
                            {
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    if (drawn0 >= comboRequired0)
                                    {
                                        successfulCombinations += table[drawn, drawn0];
                                    }
                                    else if (comboGroup._targetDraw == drawn)
                                    {
                                        table[drawn, drawn0] = 0;
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesCroppedTable2D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups[0]._groupSize;
                    uint all1 = cardGroups[1]._groupSize;
                    uint allSum = all0 + all1;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = required0 + 1;
                    uint arrayLength1 = required1 + 1;

                    ulong[,,] table = new ulong[totalCardDraws + 1, arrayLength0, arrayLength1];
                    table[0, 0, 0] = 1;

                    uint drawn0;
                    uint drawn1;

                    uint remaining0;
                    uint remaining1;

                    ulong totalCombinations = 1;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1; drawn <= totalCardDraws; drawn++)
                    {
                        uint previouslyDrawn = drawn - 1;
                        uint cardsInDeck = deckSize - previouslyDrawn;

                        for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                        {
                            remaining0 = drawn0 < required0 ? all0 - drawn0 : 0;
                            for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                            {
                                ulong previousSequences = table[previouslyDrawn, drawn0, drawn1];
                                if (previousSequences == 0)
                                    continue;

                                remaining1 = drawn1 < required1 ? all1 - drawn1 : 0;
                                uint remainingSum = remaining0 + remaining1;

                                if (cardsInDeck > remainingSum)
                                    table[drawn, drawn0, drawn1] += previousSequences * (cardsInDeck - remainingSum);
                                if (drawn0 < required0)
                                    table[drawn, drawn0 + 1, drawn1] += previousSequences * remaining0;
                                if (drawn1 < required1)
                                    table[drawn, drawn0, drawn1 + 1] += previousSequences * remaining1;
                            }
                        }

                        totalCombinations *= cardsInDeck;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount[0];
                            uint comboRequired1 = comboGroup._requiredCount[1];

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                        successfulCombinations += table[drawn, drawn0, drawn1];
                                    }
                                }
                            }
                            else
                            {
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        if (check0 && (drawn1 >= comboRequired1))
                                        {
                                            successfulCombinations += table[drawn, drawn0, drawn1];
                                        }
                                        else if (comboGroup._targetDraw == drawn)
                                        {
                                            table[drawn, drawn0, drawn1] = 0;
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesCroppedTable3D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint required2 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        if (comboGroup._requiredCount.Length > 2)
                            required2 = Math.Max(required2, comboGroup._requiredCount[2]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups[0]._groupSize;
                    uint all1 = cardGroups[1]._groupSize;
                    uint all2 = cardGroups[2]._groupSize;
                    uint allSum = all0 + all1 + all2;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = required0 + 1;
                    uint arrayLength1 = required1 + 1;
                    uint arrayLength2 = required2 + 1;

                    ulong[,,,] table = new ulong[totalCardDraws + 1, arrayLength0, arrayLength1, arrayLength2];
                    table[0, 0, 0, 0] = 1;

                    uint drawn0;
                    uint drawn1;
                    uint drawn2;

                    uint remaining0;
                    uint remaining1;
                    uint remaining2;

                    ulong totalCombinations = 1;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1; drawn <= totalCardDraws; drawn++)
                    {
                        uint previouslyDrawn = drawn - 1;
                        uint cardsInDeck = deckSize - previouslyDrawn;

                        for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                        {
                            remaining0 = drawn0 < required0 ? all0 - drawn0 : 0;
                            for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                            {
                                remaining1 = drawn1 < required1 ? all1 - drawn1 : 0;
                                for (drawn2 = 0; drawn2 < arrayLength2; drawn2++)
                                {
                                    ulong previousSequences = table[previouslyDrawn, drawn0, drawn1, drawn2];
                                    if (previousSequences == 0)
                                        continue;

                                    remaining2 = drawn2 < required2 ? all2 - drawn2 : 0;
                                    uint remainingSum = remaining0 + remaining1 + remaining2;

                                    if (cardsInDeck > remainingSum)
                                        table[drawn, drawn0, drawn1, drawn2] += previousSequences * (cardsInDeck - remainingSum);
                                    if (drawn0 < required0)
                                        table[drawn, drawn0 + 1, drawn1, drawn2] += previousSequences * remaining0;
                                    if (drawn1 < required1)
                                        table[drawn, drawn0, drawn1 + 1, drawn2] += previousSequences * remaining1;
                                    if (drawn2 < required2)
                                        table[drawn, drawn0, drawn1, drawn2 + 1] += previousSequences * remaining2;
                                }
                            }
                        }

                        totalCombinations *= cardsInDeck;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount[0];
                            uint comboRequired1 = comboGroup._requiredCount[1];
                            uint comboRequired2 = comboGroup._requiredCount[2];

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                        for (drawn2 = comboRequired2; drawn2 < arrayLength2; drawn2++)
                                        {
                                            successfulCombinations += table[drawn, drawn0, drawn1, drawn2];
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        bool check1 = drawn1 >= comboRequired1;
                                        for (drawn2 = 0; drawn2 < arrayLength2; drawn2++)
                                        {
                                            if (check0 && check1 && (drawn2 >= comboRequired2))
                                            {
                                                successfulCombinations += table[drawn, drawn0, drawn1, drawn2];
                                            }
                                            else if (comboGroup._targetDraw == drawn)
                                            {
                                                table[drawn, drawn0, drawn1, drawn2] = 0;
                                            }
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesCroppedTable4D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint required2 = 0;
                    uint required3 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        if (comboGroup._requiredCount.Length > 2)
                            required2 = Math.Max(required2, comboGroup._requiredCount[2]);
                        if (comboGroup._requiredCount.Length > 3)
                            required3 = Math.Max(required3, comboGroup._requiredCount[3]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups[0]._groupSize;
                    uint all1 = cardGroups[1]._groupSize;
                    uint all2 = cardGroups[2]._groupSize;
                    uint all3 = cardGroups[3]._groupSize;
                    uint allSum = all0 + all1 + all2 + all3;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("------Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = required0 + 1;
                    uint arrayLength1 = required1 + 1;
                    uint arrayLength2 = required2 + 1;
                    uint arrayLength3 = required3 + 1;

                    ulong[,,,,] table = new ulong[totalCardDraws + 1, arrayLength0, arrayLength1, arrayLength2, arrayLength3];
                    table[0, 0, 0, 0, 0] = 1;

                    uint drawn0;
                    uint drawn1;
                    uint drawn2;
                    uint drawn3;

                    uint remaining0;
                    uint remaining1;
                    uint remaining2;
                    uint remaining3;

                    ulong totalCombinations = 1;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1; drawn <= totalCardDraws; drawn++)
                    {
                        uint previouslyDrawn = drawn - 1;
                        uint cardsInDeck = deckSize - previouslyDrawn;

                        for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                        {
                            remaining0 = drawn0 < required0 ? all0 - drawn0 : 0;
                            for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                            {
                                remaining1 = drawn1 < required1 ? all1 - drawn1 : 0;
                                for (drawn2 = 0; drawn2 < arrayLength2; drawn2++)
                                {
                                    remaining2 = drawn2 < required2 ? all2 - drawn2 : 0;
                                    for (drawn3 = 0; drawn3 < arrayLength3; drawn3++)
                                    {
                                        ulong previousSequences = table[previouslyDrawn, drawn0, drawn1, drawn2, drawn3];
                                        if (previousSequences == 0)
                                            continue;

                                        remaining3 = drawn3 < required3 ? all3 - drawn3 : 0;
                                        uint remainingSum = remaining0 + remaining1 + remaining2 + remaining3;

                                        if (cardsInDeck > remainingSum)
                                            table[drawn, drawn0, drawn1, drawn2, drawn3] += previousSequences * (cardsInDeck - remainingSum);
                                        if (drawn0 < required0)
                                            table[drawn, drawn0 + 1, drawn1, drawn2, drawn3] += previousSequences * remaining0;
                                        if (drawn1 < required1)
                                            table[drawn, drawn0, drawn1 + 1, drawn2, drawn3] += previousSequences * remaining1;
                                        if (drawn2 < required2)
                                            table[drawn, drawn0, drawn1, drawn2 + 1, drawn3] += previousSequences * remaining2;
                                        if (drawn3 < required3)
                                            table[drawn, drawn0, drawn1, drawn2, drawn3 + 1] += previousSequences * remaining3;
                                    }
                                }
                            }
                        }

                        totalCombinations *= cardsInDeck;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount[0];
                            uint comboRequired1 = comboGroup._requiredCount[1];
                            uint comboRequired2 = comboGroup._requiredCount[2];
                            uint comboRequired3 = comboGroup._requiredCount[3];

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                        for (drawn2 = comboRequired2; drawn2 < arrayLength2; drawn2++)
                                        {
                                            for (drawn3 = comboRequired3; drawn3 < arrayLength3; drawn3++)
                                            {
                                                successfulCombinations += table[drawn, drawn0, drawn1, drawn2, drawn3];
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        bool check1 = drawn1 >= comboRequired1;
                                        for (drawn2 = 0; drawn2 < arrayLength2; drawn2++)
                                        {
                                            bool check2 = drawn2 >= comboRequired2;
                                            for (drawn3 = 0; drawn3 < arrayLength3; drawn3++)
                                            {
                                                if (check0 && check1 && check2 && (drawn3 >= comboRequired3))
                                                {
                                                    successfulCombinations += table[drawn, drawn0, drawn1, drawn2, drawn3];
                                                }
                                                else if (comboGroup._targetDraw == drawn)
                                                {
                                                    table[drawn, drawn0, drawn1, drawn2, drawn3] = 0;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }
            }

            (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesOptimized()
            {
                switch (cardGroups.Length)
                {
                    case 0:
                        throw new Exception("The cardGroups array is empty.");
                    case 1:
                        return CalculateComboProbabilitiesOptimized1D();
                    case 2:
                        return CalculateComboProbabilitiesOptimized2D();
                    case 3:
                        return CalculateComboProbabilitiesOptimized3D();
                    case 4:
                        return CalculateComboProbabilitiesOptimized4D();
                    default:
                        throw new Exception("This algorithm doesn't support more than 4 combo groups.");
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesOptimized1D()
                {
                    uint required0 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups.Length > 0 ? cardGroups[0]._groupSize : 0;

                    if (all0 > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = all0;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("-Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = required0 + 1;

                    ulong[] table = new ulong[arrayLength0];
                    table[0] = 1;

                    uint drawn0;

                    uint remaining0;

                    ulong totalCombinations = 1;
                    ulong previousSequences;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1; drawn <= totalCardDraws; drawn++)
                    {
                        uint previouslyDrawn = drawn - 1;
                        uint cardsInDeck = deckSize - previouslyDrawn;
                        int index = table.Length;

                        for (drawn0 = required0; drawn0 < uint.MaxValue; drawn0--)
                        {
                            previousSequences = table[--index];
                            if (previousSequences == 0)
                                continue;

                            remaining0 = drawn0 < required0 ? all0 - drawn0 : 0;

                            if (cardsInDeck > remaining0)
                                table[index] += previousSequences * (cardsInDeck - remaining0 - 1);
                            else
                                table[index] -= previousSequences;
                            if (drawn0 < required0)
                                table[index + 1] += previousSequences * remaining0;
                        }

                        totalCombinations *= cardsInDeck;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount.Length > 0 ? comboGroup._requiredCount[0] : 0;

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    successfulCombinations += table[drawn0];
                                }
                            }
                            else
                            {
                                index = 0;
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    if (drawn0 >= comboRequired0)
                                    {
                                        successfulCombinations += table[index++];
                                    }
                                    else if (comboGroup._targetDraw == drawn)
                                    {
                                        table[index++] = 0;
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesOptimized2D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups.Length > 0 ? cardGroups[0]._groupSize : 0;
                    uint all1 = cardGroups.Length > 1 ? cardGroups[1]._groupSize : 0;
                    uint allSum = all0 + all1;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("--Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = required0 + 1;
                    uint arrayLength1 = required1 + 1;

                    uint arrayOffset0 = arrayLength1;

                    ulong[] table = new ulong[arrayOffset0 * arrayLength0];
                    table[0] = 1;

                    uint drawn0;
                    uint drawn1;

                    uint remaining0;
                    uint remaining1;
                    uint remainingSum;

                    ulong totalCombinations = 1;
                    ulong previousSequences;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1; drawn <= totalCardDraws; drawn++)
                    {
                        uint previouslyDrawn = drawn - 1;
                        uint cardsInDeck = deckSize - previouslyDrawn;
                        int index = table.Length;

                        for (drawn0 = required0; drawn0 < uint.MaxValue; drawn0--)
                        {
                            remaining0 = drawn0 < required0 ? all0 - drawn0 : 0;
                            for (drawn1 = required1; drawn1 < uint.MaxValue; drawn1--)
                            {
                                previousSequences = table[--index];
                                if (previousSequences == 0)
                                    continue;

                                remaining1 = drawn1 < required1 ? all1 - drawn1 : 0;
                                remainingSum = remaining0 + remaining1;

                                if (cardsInDeck > remainingSum)
                                    table[index] += previousSequences * (cardsInDeck - remainingSum - 1);
                                else
                                    table[index] -= previousSequences;
                                if (drawn1 < required1)
                                    table[index + 1] += previousSequences * remaining1;
                                if (drawn0 < required0)
                                    table[index + arrayOffset0] += previousSequences * remaining0;
                            }
                        }

                        totalCombinations *= cardsInDeck;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount.Length > 0 ? comboGroup._requiredCount[0] : 0;
                            uint comboRequired1 = comboGroup._requiredCount.Length > 1 ? comboGroup._requiredCount[1] : 0;

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                            successfulCombinations += table[drawn0 * arrayOffset0 + drawn1];
                                    }
                                }
                            }
                            else
                            {
                                index = 0;
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        if (check0 && (drawn1 >= comboRequired1))
                                        {
                                            successfulCombinations += table[index++];
                                        }
                                        else if (comboGroup._targetDraw == drawn)
                                        {
                                            table[index++] = 0;
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesOptimized3D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint required2 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        if (comboGroup._requiredCount.Length > 2)
                            required2 = Math.Max(required2, comboGroup._requiredCount[2]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups.Length > 0 ? cardGroups[0]._groupSize : 0;
                    uint all1 = cardGroups.Length > 1 ? cardGroups[1]._groupSize : 0;
                    uint all2 = cardGroups.Length > 2 ? cardGroups[2]._groupSize : 0;
                    uint allSum = all0 + all1 + all2;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("---Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = required0 + 1;
                    uint arrayLength1 = required1 + 1;
                    uint arrayLength2 = required2 + 1;

                    uint arrayOffset1 = arrayLength2;
                    uint arrayOffset0 = arrayOffset1 * arrayLength1;

                    ulong[] table = new ulong[arrayOffset0 * arrayLength0];
                    table[0] = 1;

                    uint drawn0;
                    uint drawn1;
                    uint drawn2;

                    uint remaining0;
                    uint remaining1;
                    uint remaining2;
                    uint remainingSum;

                    ulong totalCombinations = 1;
                    ulong previousSequences;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1; drawn <= totalCardDraws; drawn++)
                    {
                        uint previouslyDrawn = drawn - 1;
                        uint cardsInDeck = deckSize - previouslyDrawn;
                        int index = table.Length;

                        for (drawn0 = required0; drawn0 < uint.MaxValue; drawn0--)
                        {
                            remaining0 = drawn0 < required0 ? all0 - drawn0 : 0;
                            for (drawn1 = required1; drawn1 < uint.MaxValue; drawn1--)
                            {
                                remaining1 = drawn1 < required1 ? all1 - drawn1 : 0;
                                for (drawn2 = required2; drawn2 < uint.MaxValue; drawn2--)
                                {
                                    previousSequences = table[--index];
                                    if (previousSequences == 0)
                                        continue;
                                    remaining2 = drawn2 < required2 ? all2 - drawn2 : 0;

                                    remainingSum = remaining0 + remaining1 + remaining2;

                                    if (cardsInDeck > remainingSum)
                                        table[index] += previousSequences * (cardsInDeck - remainingSum - 1);
                                    else
                                        table[index] -= previousSequences;
                                    if (drawn2 < required2)
                                        table[index + 1] += previousSequences * remaining2;
                                    if (drawn1 < required1)
                                        table[index + arrayOffset1] += previousSequences * remaining1;
                                    if (drawn0 < required0)
                                        table[index + arrayOffset0] += previousSequences * remaining0;
                                }
                            }
                        }

                        totalCombinations *= cardsInDeck;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount.Length > 0 ? comboGroup._requiredCount[0] : 0;
                            uint comboRequired1 = comboGroup._requiredCount.Length > 1 ? comboGroup._requiredCount[1] : 0;
                            uint comboRequired2 = comboGroup._requiredCount.Length > 2 ? comboGroup._requiredCount[2] : 0;

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                        for (drawn2 = comboRequired2; drawn2 < arrayLength2; drawn2++)
                                        {
                                            successfulCombinations += table[drawn0 * arrayOffset0 + drawn1 * arrayOffset1 + drawn2];
                                        }
                                    }
                                }
                            }
                            else
                            {
                                index = 0;
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        bool check1 = drawn1 >= comboRequired1;
                                        for (drawn2 = 0; drawn2 < arrayLength2; drawn2++)
                                        {
                                            if (check0 && check1 && (drawn2 >= comboRequired2))
                                            {
                                                successfulCombinations += table[index++];
                                            }
                                            else if (comboGroup._targetDraw == drawn)
                                            {
                                                table[index++] = 0;
                                            }
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }

                (ProbabilitySet[] probabilitySets, object data) CalculateComboProbabilitiesOptimized4D()
                {
                    uint required0 = 0;
                    uint required1 = 0;
                    uint required2 = 0;
                    uint required3 = 0;
                    uint totalCardDraws = 0;

                    foreach (var comboGroup in comboGroups)
                    {
                        if (comboGroup._requiredCount.Length > 0)
                            required0 = Math.Max(required0, comboGroup._requiredCount[0]);
                        if (comboGroup._requiredCount.Length > 1)
                            required1 = Math.Max(required1, comboGroup._requiredCount[1]);
                        if (comboGroup._requiredCount.Length > 2)
                            required2 = Math.Max(required2, comboGroup._requiredCount[2]);
                        if (comboGroup._requiredCount.Length > 3)
                            required3 = Math.Max(required3, comboGroup._requiredCount[3]);
                        totalCardDraws = Math.Max(totalCardDraws, comboGroup._targetDraw);
                    }

                    uint all0 = cardGroups.Length > 0 ? cardGroups[0]._groupSize : 0;
                    uint all1 = cardGroups.Length > 1 ? cardGroups[1]._groupSize : 0;
                    uint all2 = cardGroups.Length > 2 ? cardGroups[2]._groupSize : 0;
                    uint all3 = cardGroups.Length > 3 ? cardGroups[3]._groupSize : 0;
                    uint allSum = all0 + all1 + all2 + all3;

                    if (allSum > deckSize)
                    {
                        Debug.LogWarning("Total number of cards in card groups exceeded the number of cards in the deck. Increased the deck size accordingly.");
                        deckSize = allSum;
                    }

                    if (totalCardDraws > deckSize)
                    {
                        Debug.LogWarning("----Total card draws can't exceed the deck size. The number of draws has been reduced to the deck size.");
                        totalCardDraws = deckSize;
                    }

                    uint arrayLength0 = required0 + 1;
                    uint arrayLength1 = required1 + 1;
                    uint arrayLength2 = required2 + 1;
                    uint arrayLength3 = required3 + 1;

                    uint arrayOffset2 = arrayLength3;
                    uint arrayOffset1 = arrayOffset2 * arrayLength2;
                    uint arrayOffset0 = arrayOffset1 * arrayLength1;

                    ulong[] table = new ulong[arrayOffset0 * arrayLength0];
                    table[0] = 1;

                    uint drawn0;
                    uint drawn1;
                    uint drawn2;
                    uint drawn3;

                    uint remaining0;
                    uint remaining1;
                    uint remaining2;
                    uint remaining3;
                    uint remainingSum;

                    ulong totalCombinations = 1;
                    ulong previousSequences;

                    ProbabilitySet[] output = new ProbabilitySet[comboGroups.Length];
                    for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                    {
                        output[comboGroupIndex] = new ProbabilitySet(comboGroups[comboGroupIndex]);
                    }
                    for (uint drawn = 1; drawn <= totalCardDraws; drawn++)
                    {
                        uint previouslyDrawn = drawn - 1;
                        uint cardsInDeck = deckSize - previouslyDrawn;
                        int index = table.Length;

                        for (drawn0 = required0; drawn0 < uint.MaxValue; drawn0--)
                        {
                            remaining0 = drawn0 < required0 ? all0 - drawn0 : 0;
                            for (drawn1 = required1; drawn1 < uint.MaxValue; drawn1--)
                            {
                                remaining1 = drawn1 < required1 ? all1 - drawn1 : 0;
                                for (drawn2 = required2; drawn2 < uint.MaxValue; drawn2--)
                                {
                                    remaining2 = drawn2 < required2 ? all2 - drawn2 : 0;
                                    for (drawn3 = required3; drawn3 < uint.MaxValue; drawn3--)
                                    {
                                        index--;
                                        previousSequences = table[index];
                                        if (previousSequences == 0)
                                            continue;

                                        remaining3 = drawn3 < required3 ? all3 - drawn3 : 0;
                                        remainingSum = remaining0 + remaining1 + remaining2 + remaining3;

                                        if (cardsInDeck > remainingSum)
                                            table[index] += previousSequences * (cardsInDeck - remainingSum - 1);
                                        else
                                            table[index] -= previousSequences;
                                        if (drawn3 < required3)
                                            table[index + 1] += previousSequences * remaining3;
                                        if (drawn2 < required2)
                                            table[index + arrayOffset2] += previousSequences * remaining2;
                                        if (drawn1 < required1)
                                            table[index + arrayOffset1] += previousSequences * remaining1;
                                        if (drawn0 < required0)
                                            table[index + arrayOffset0] += previousSequences * remaining0;
                                    }
                                }
                            }
                        }

                        totalCombinations *= cardsInDeck;

                        for (int comboGroupIndex = 0; comboGroupIndex < comboGroups.Length; comboGroupIndex++)
                        {
                            var comboGroup = comboGroups[comboGroupIndex];
                            if (!comboGroup._enabled)
                                continue;

                            ulong successfulCombinations = 0;

                            uint comboRequired0 = comboGroup._requiredCount.Length > 0 ? comboGroup._requiredCount[0] : 0;
                            uint comboRequired1 = comboGroup._requiredCount.Length > 1 ? comboGroup._requiredCount[1] : 0;
                            uint comboRequired2 = comboGroup._requiredCount.Length > 2 ? comboGroup._requiredCount[2] : 0;
                            uint comboRequired3 = comboGroup._requiredCount.Length > 3 ? comboGroup._requiredCount[3] : 0;

                            if (comboGroup._targetDraw != drawn)
                            {
                                for (drawn0 = comboRequired0; drawn0 < arrayLength0; drawn0++)
                                {
                                    for (drawn1 = comboRequired1; drawn1 < arrayLength1; drawn1++)
                                    {
                                        for (drawn2 = comboRequired2; drawn2 < arrayLength2; drawn2++)
                                        {
                                            for (drawn3 = comboRequired3; drawn3 < arrayLength3; drawn3++)
                                            {
                                                successfulCombinations += table[drawn0 * arrayOffset0 + drawn1 * arrayOffset1 + drawn2 * arrayOffset2 + drawn3];
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                index = 0;
                                for (drawn0 = 0; drawn0 < arrayLength0; drawn0++)
                                {
                                    bool check0 = drawn0 >= comboRequired0;
                                    for (drawn1 = 0; drawn1 < arrayLength1; drawn1++)
                                    {
                                        bool check1 = drawn1 >= comboRequired1;
                                        for (drawn2 = 0; drawn2 < arrayLength2; drawn2++)
                                        {
                                            bool check2 = drawn2 >= comboRequired2;
                                            for (drawn3 = 0; drawn3 < arrayLength3; drawn3++)
                                            {
                                                if (check0 && check1 && check2 && (drawn3 >= comboRequired3))
                                                {
                                                    successfulCombinations += table[index++];
                                                }
                                                else if (comboGroup._targetDraw == drawn)
                                                {
                                                    table[index++] = 0;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (comboGroup._targetDraw > previouslyDrawn)
                                output[comboGroupIndex]._probabilitiesByDraw[previouslyDrawn] = new Probability(successfulCombinations: successfulCombinations, totalCombinations: totalCombinations);
                        }
                    }
                    return (output, table);
                }
            }
        }
    }
}
