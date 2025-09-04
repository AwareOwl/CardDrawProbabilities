using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;


namespace DeckUtilities.Tests
{
    public class AlgorithmTests
    {
        [Test(Description = "Tests run on the scriptable objects.")]
        public void RunScripableObjectTests()
        {
            var allComboProbabilityTests = AssetUtility.GetAllAssetsOfType<ComboProbabilityTestData>();
            foreach (var comboProbability in allComboProbabilityTests)
            {
                Assert.That(comboProbability.Validate(), Is.True, $"{comboProbability.name} returned invalid output.");
            }
        }

        [Test(Description = "A brute force test that checks if different algorithm versions return the same output for the same inputs. Checks deck size range 4-8.")]
        public void BruteForceComparisonTestSmall()
        {
            BruteForceComparisonTest(4, 8);
        }

        [Test(Description = "A brute force test that checks if different algorithm versions return the same output for the same inputs. Checks deck size range 9-16.")]
        public void BruteForceComparisonTestMedium()
        {
            BruteForceComparisonTest(9, 16);
        }

        [Test(Description = "A brute force test that checks if different algorithm versions return the same output for the same inputs. Checks deck size range 23-24.")]
        public void BruteForceComparisonTestBig()
        {
            BruteForceComparisonTest(23, 24);
        }


        /// <summary>
        /// A brute force test that checks if different algorithm versions return the same output for the same inputs.
        /// </summary>
        /// <param name="minDeckSize">Minimal deck size from the range to check.</param>
        /// <param name="maxDeckSize">Maximal deck size from the range to check.</param>
        public void BruteForceComparisonTest(uint minDeckSize, uint maxDeckSize)
        {
            System.GC.Collect();
            uint uniqueInputSets = 0;
            CardGroup[] cardGroups4D = new CardGroup[] { new CardGroup("GA", 4), new CardGroup("GB", 3), new CardGroup("GC", 2), new CardGroup("GD", 1) };
            CardGroup[] cardGroups3D = new CardGroup[] { cardGroups4D[0], cardGroups4D[1], cardGroups4D[2] };
            CardGroup[] cardGroups2D = new CardGroup[] { cardGroups4D[0], cardGroups4D[1]};
            CardGroup[] cardGroups1D = new CardGroup[] { cardGroups4D[0]};
            ComboGroup[] comboGroups = new ComboGroup[] { new ComboGroup("CA", new uint[] { 1, 0, 0, 0 }) };
            ProbabilitySet[] fullTableProbabilities;
            ProbabilitySet[] croppedTableProbabilities;
            ProbabilitySet[] optimizedProbabilities;
            for (uint deckSize = minDeckSize; deckSize <= maxDeckSize; deckSize += 2)
            {
                comboGroups[0]._targetDraw = deckSize;
                for (uint ga = 1; ga < 10 && ga < deckSize; ga += 2)
                {
                    cardGroups4D[0]._groupSize = ga;
                    for (uint gb = 0; gb < 10 && ga + gb < deckSize; gb += 2)
                    {
                        cardGroups4D[1]._groupSize = gb;
                        for (uint gc = 0; gc < 10 && ga + gb + gc < deckSize; gc += 2)
                        {
                            cardGroups4D[2]._groupSize = gc;
                            for (uint gd = 0; gd < 10 && ga + gb + gc + gd < deckSize; gd += 2)
                            {
                                cardGroups4D[3]._groupSize = gd;
                                for (uint ca = 1; ca <= ga; ca++)
                                {
                                    comboGroups[0]._requiredCount[0] = ca;
                                    RunAlgorithms(cardGroups1D);
                                    for (int setIndex = 0; setIndex < fullTableProbabilities.Length; setIndex++)
                                    {
                                        Assert.That(fullTableProbabilities[setIndex].Compare(croppedTableProbabilities[setIndex]), Is.True,
                                            $"Invalid output for: deckSize: {deckSize}; groups: [{ga}]; combos: [{ca}]");
                                        Assert.That(fullTableProbabilities[setIndex].Compare(optimizedProbabilities[setIndex]), Is.True,
                                            $"Invalid output for: deckSize: {deckSize}; groups: [{ga}]; combos: [{ca}]");
                                    }
                                    for (uint cb = 0; cb <= gb; cb++)
                                    {
                                        comboGroups[0]._requiredCount[1] = cb;
                                        RunAlgorithms(cardGroups2D);
                                        for (int setIndex = 0; setIndex < fullTableProbabilities.Length; setIndex++)
                                        {
                                            Assert.That(fullTableProbabilities[setIndex].Compare(croppedTableProbabilities[setIndex]), Is.True,
                                                $"Invalid output for: deckSize: {deckSize}; groups: [{ga}, {gb}]; combos: [{ca}, {cb}]");
                                            Assert.That(fullTableProbabilities[setIndex].Compare(optimizedProbabilities[setIndex]), Is.True,
                                                $"Invalid output for: deckSize: {deckSize}; groups: [{ga}, {gb}]; combos: [{ca}, {cb}]");
                                        }
                                        for (uint cc = 0; cc <= gc; cc++)
                                        {
                                            comboGroups[0]._requiredCount[2] = cc;
                                            RunAlgorithms(cardGroups3D);
                                            for (int setIndex = 0; setIndex < fullTableProbabilities.Length; setIndex++)
                                            {
                                                Assert.That(fullTableProbabilities[setIndex].Compare(croppedTableProbabilities[setIndex]), Is.True,
                                                    $"Invalid output for: deckSize: {deckSize}; groups: [{ga}, {gb}, {gc}]; combos: [{ca}, {cb}, {cc}]");
                                                Assert.That(fullTableProbabilities[setIndex].Compare(optimizedProbabilities[setIndex]), Is.True,
                                                    $"Invalid output for: deckSize: {deckSize}; groups: [{ga}, {gb}, {gc}]; combos: [{ca}, {cb}, {cc}]");
                                            }
                                            for (uint cd = 0; cd <= gd; cd++)
                                            {
                                                comboGroups[0]._requiredCount[3] = cd;
                                                RunAlgorithms(cardGroups4D);
                                                for (int setIndex = 0; setIndex < fullTableProbabilities.Length; setIndex++)
                                                {
                                                    Assert.That(fullTableProbabilities[setIndex].Compare(croppedTableProbabilities[setIndex]), Is.True,
                                                        $"Invalid output for: deckSize: {deckSize}; groups: [{ga}, {gb}, {gc}, {gd}]; combos: [{ca}, {cb}, {cc}, {cd}]");
                                                    Assert.That(fullTableProbabilities[setIndex].Compare(optimizedProbabilities[setIndex]), Is.True,
                                                        $"Invalid output for: deckSize: {deckSize}; groups: [{ga}, {gb}, {gc}, {gd}]; combos: [{ca}, {cb}, {cc}, {cd}]");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                void RunAlgorithms(CardGroup[] cardGroups)
                {
                    uniqueInputSets++;
                    fullTableProbabilities = ComboProbabilityUtilities.CalculateComboProbabilities(deckSize, cardGroups, comboGroups,
                        ComboProbabilityUtilities.AlgorithmVersion.FullTable).probabilitySets;
                    croppedTableProbabilities = ComboProbabilityUtilities.CalculateComboProbabilities(deckSize, cardGroups, comboGroups,
                        ComboProbabilityUtilities.AlgorithmVersion.CroppedTable).probabilitySets;
                    optimizedProbabilities = ComboProbabilityUtilities.CalculateComboProbabilities(deckSize, cardGroups, comboGroups,
                        ComboProbabilityUtilities.AlgorithmVersion.Optimized).probabilitySets;
                }
            }
            Debug.Log($"Unique input sets: {uniqueInputSets}");
        }

        static List<double> fullTableExecutionTimes = new List<double>();
        static List<double> croppedTableExecutionTimes = new List<double>();
        static List<double> optimizedExecutionTimes = new List<double>();

        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 4-8). Tests 503 unique input sets.")]
        public void BruteForceSpeedComparisonTestSmall() => BruteForceSpeedComparison(1, 4, 8);

        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 4-8). Tests 503 unique input sets.")]
        public void BruteForceSpeedComparisonTestSmallX25() => BruteForceSpeedComparison(25, 4, 8);

        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 4-8). Tests 503 unique input sets.")]
        public void BruteForceSpeedComparisonTestSmallX100() => BruteForceSpeedComparison(100, 4, 8);

        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 15-16). Tests 11826 unique input sets.")]
        public async Task BruteForceSpeedComparisonTestSmallX100AsyncTasks() => await BruteForceSpeedComparisonAsyncTasks(100, 4, 8);

        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 15-16). Tests 11826 unique input sets.")]
        public void BruteForceSpeedComparisonTestMedium() => BruteForceSpeedComparison(1, 15, 16);

        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 15-16). Tests 11826 unique input sets.")]
        public void BruteForceSpeedComparisonTestMediumX5() => BruteForceSpeedComparison(5, 15, 16);

        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 15-16). Tests 11826 unique input sets.")]
        public void BruteForceSpeedComparisonTestMediumX10() => BruteForceSpeedComparison(10, 15, 16);
        
        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 21-22). Tests 127699 unique input sets.")]
        public void BruteForceSpeedComparisonTestBig() => BruteForceSpeedComparison(1, 21, 22);

        [Test(Description = "Brute force test meant to check the execution time for tests from given range (deck size: 21-22). Tests 127699 unique input sets.")]
        public void BruteForceSpeedComparisonTestBigX3() => BruteForceSpeedComparison(3, 21, 22);

        /// <summary>
        /// Brute force test meant to check the execution time for tests from given range.
        /// </summary>
        /// <param name="iterations">Number of test iterations.</param>
        /// <param name="minDeckSize">Minimal deck size from the range to check.</param>
        /// <param name="maxDeckSize">Maximal deck size from the range to check.</param>
        void BruteForceSpeedComparison (int iterations, uint minDeckSize, uint maxDeckSize)
        {
            fullTableExecutionTimes.Clear();
            croppedTableExecutionTimes.Clear();
            optimizedExecutionTimes.Clear();

            System.GC.Collect();
            var tasks = new Task<(double fullTableExecutionTime, double croppedTableExecutionTime, double optimizedExecutionTimes)>[iterations]; 
            for (int i = 0; i < iterations; i++)
            {
                var result = BruteForceSpeedComparisonTest(minDeckSize, maxDeckSize);
                fullTableExecutionTimes.Add(result.fullTableExecutionTime);
                croppedTableExecutionTimes.Add(result.croppedTableExecutionTime);
                optimizedExecutionTimes.Add(result.optimizedExecutionTimes);
            }

            LogExecutionTimesSummary(fullTableExecutionTimes, ComboProbabilityUtilities.AlgorithmVersion.FullTable);
            LogExecutionTimesSummary(croppedTableExecutionTimes, ComboProbabilityUtilities.AlgorithmVersion.CroppedTable);
            LogExecutionTimesSummary(optimizedExecutionTimes, ComboProbabilityUtilities.AlgorithmVersion.Optimized);
        }

        /// <summary>
        /// Brute force test meant to check the execution time for tests from given range.
        /// </summary>
        /// <param name="iterations">Number of test iterations.</param>
        /// <param name="minDeckSize">Minimal deck size from the range to check.</param>
        /// <param name="maxDeckSize">Maximal deck size from the range to check.</param>
        async Task BruteForceSpeedComparisonAsyncTasks(int iterations, uint minDeckSize, uint maxDeckSize)
        {
            fullTableExecutionTimes.Clear();
            croppedTableExecutionTimes.Clear();
            optimizedExecutionTimes.Clear();

            System.GC.Collect();
            var tasks = new Task<(double fullTableExecutionTime, double croppedTableExecutionTime, double optimizedExecutionTimes)>[iterations];
            for (int i = 0; i < iterations; i++)
            {
                tasks[i] = Task.Run(() => BruteForceSpeedComparisonTest(minDeckSize, maxDeckSize));
            }

            await Task.WhenAll(tasks);
            await Awaitable.MainThreadAsync();

            foreach (var task in tasks)
            {
                fullTableExecutionTimes.Add(task.Result.fullTableExecutionTime);
                croppedTableExecutionTimes.Add(task.Result.croppedTableExecutionTime);
                optimizedExecutionTimes.Add(task.Result.optimizedExecutionTimes);
            }
            LogExecutionTimesSummary(fullTableExecutionTimes, ComboProbabilityUtilities.AlgorithmVersion.FullTable);
            LogExecutionTimesSummary(croppedTableExecutionTimes, ComboProbabilityUtilities.AlgorithmVersion.CroppedTable);
            LogExecutionTimesSummary(optimizedExecutionTimes, ComboProbabilityUtilities.AlgorithmVersion.Optimized);
        }

        void LogExecutionTimesSummary (List<double> executionTimes, ComboProbabilityUtilities.AlgorithmVersion algorithmVersion)
        {
            executionTimes.Sort();
            double total = 0;
            int iterations = executionTimes.Count;
            for (int i = 0; i < iterations; i++)
            {
                total += executionTimes[i];
            }
            Debug.Log($"AlgorithmVersion: {algorithmVersion}; Min: {executionTimes[0]}ms, Max: {executionTimes[iterations - 1]}ms, Median: {executionTimes[iterations / 2]}ms, Avg: {(float)total / iterations}ms");
        }

        /// <summary>
        /// Brute force test meant to check the execution time for tests from given range.
        /// </summary>
        /// <param name="minDeckSize">Minimal deck size from the range to check.</param>
        /// <param name="maxDeckSize">Maximal deck size from the range to check.</param>
        public (double fullTableExecutionTime, double croppedTableExecutionTime, double optimizedExecutionTimes) BruteForceSpeedComparisonTest(uint startingDeckSize, uint endingDeckSize)
        {
            CardGroup[] cardGroups = new CardGroup[] { new CardGroup("GA", 4), new CardGroup("GB", 3), new CardGroup("GC", 2), new CardGroup("GD", 1) };
            ComboGroup[] comboGroups = new ComboGroup[] { new ComboGroup("CA", new uint[] { 1, 0, 0, 0 }) };

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (uint deckSize = startingDeckSize; deckSize <= endingDeckSize; deckSize += 2)
            {
                comboGroups[0]._targetDraw = deckSize;
                for (uint ga = 1; ga < deckSize && ga < deckSize; ga += 2)
                {
                    cardGroups[0]._groupSize = ga;
                    for (uint gb = 0; gb < deckSize && ga + gb < deckSize; gb += 2)
                    {
                        cardGroups[1]._groupSize = gb;
                        for (uint gc = 0; gc < deckSize && ga + gb + gc < deckSize; gc += 2)
                        {
                            cardGroups[2]._groupSize = gc;
                            for (uint gd = 0; gd < deckSize && ga + gb + gc + gd < deckSize; gd += 2)
                            {
                                cardGroups[3]._groupSize = gd;
                                for (uint ca = 1; ca <= ga; ca++)
                                {
                                    comboGroups[0]._requiredCount[0] = ca;
                                    for (uint cb = 0; cb <= gb; cb++)
                                    {
                                        comboGroups[0]._requiredCount[1] = cb;
                                        for (uint cc = 0; cc <= gc; cc++)
                                        {
                                            comboGroups[0]._requiredCount[2] = cc;
                                            for (uint cd = 0; cd <= gd; cd++)
                                            {
                                                comboGroups[0]._requiredCount[3] = cd;
                                                ComboProbabilityUtilities.CalculateComboProbabilities(deckSize, cardGroups, comboGroups,
                                                    ComboProbabilityUtilities.AlgorithmVersion.FullTable);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            double fullTableExecutionTime = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
            for (uint deckSize = startingDeckSize; deckSize < endingDeckSize; deckSize += 2)
            {
                comboGroups[0]._targetDraw = deckSize;
                for (uint ga = 1; ga < 10 && ga < deckSize; ga += 2)
                {
                    cardGroups[0]._groupSize = ga;
                    for (uint gb = 0; gb < 10 && ga + gb < deckSize; gb += 2)
                    {
                        cardGroups[1]._groupSize = gb;
                        for (uint gc = 0; gc < 10 && ga + gb + gc < deckSize; gc += 2)
                        {
                            cardGroups[2]._groupSize = gc;
                            for (uint gd = 0; gd < 10 && ga + gb + gc + gd < deckSize; gd += 2)
                            {
                                cardGroups[3]._groupSize = gd;
                                for (uint ca = 1; ca <= ga; ca++)
                                {
                                    comboGroups[0]._requiredCount[0] = ca;
                                    for (uint cb = 0; cb <= gb; cb++)
                                    {
                                        comboGroups[0]._requiredCount[1] = cb;
                                        for (uint cc = 0; cc <= gc; cc++)
                                        {
                                            comboGroups[0]._requiredCount[2] = cc;
                                            for (uint cd = 0; cd <= gd; cd++)
                                            {
                                                comboGroups[0]._requiredCount[3] = cd;
                                                ComboProbabilityUtilities.CalculateComboProbabilities(deckSize, cardGroups, comboGroups,
                                                    ComboProbabilityUtilities.AlgorithmVersion.CroppedTable);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            double croppedTableExecutionTime = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Restart();
            for (uint deckSize = startingDeckSize; deckSize < endingDeckSize; deckSize += 2)
            {
                comboGroups[0]._targetDraw = deckSize;
                for (uint ga = 1; ga < 10 && ga < deckSize; ga += 2)
                {
                    cardGroups[0]._groupSize = ga;
                    for (uint gb = 0; gb < 10 && ga + gb < deckSize; gb += 2)
                    {
                        cardGroups[1]._groupSize = gb;
                        for (uint gc = 0; gc < 10 && ga + gb + gc < deckSize; gc += 2)
                        {
                            cardGroups[2]._groupSize = gc;
                            for (uint gd = 0; gd < 10 && ga + gb + gc + gd < deckSize; gd += 2)
                            {
                                cardGroups[3]._groupSize = gd;
                                for (uint ca = 1; ca <= ga; ca++)
                                {
                                    comboGroups[0]._requiredCount[0] = ca;
                                    for (uint cb = 0; cb <= gb; cb++)
                                    {
                                        comboGroups[0]._requiredCount[1] = cb;
                                        for (uint cc = 0; cc <= gc; cc++)
                                        {
                                            comboGroups[0]._requiredCount[2] = cc;
                                            for (uint cd = 0; cd <= gd; cd++)
                                            {
                                                comboGroups[0]._requiredCount[3] = cd;
                                                ComboProbabilityUtilities.CalculateComboProbabilities(deckSize, cardGroups, comboGroups,
                                                    ComboProbabilityUtilities.AlgorithmVersion.Optimized);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            double optimizedExecutionTimes = stopwatch.Elapsed.TotalMilliseconds;
            stopwatch.Stop();
            return (fullTableExecutionTime, croppedTableExecutionTime, optimizedExecutionTimes);
        }
    }
}