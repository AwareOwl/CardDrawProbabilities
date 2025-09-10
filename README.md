# CardDrawProbabilities
Examples of algorithms that can be used to calculate the odds of getting particular combinations of cards by given turn. The algorithms support multiple card combinations at once, where each one has an individual turn restriction.

Example scenario: player has a deck that contains 30 cards, where 4 cards are of type A, 3 cards are of type B, and 2 cards of type C. Player needs to draw at least 2 cards A by the turn 1, and at least 1 card B and 1 card C by the turn 2. By the turn 1 player will draw 8 cards total, and they will draw another card at the start of the turn. The algorithm can calculate how likely is this scenario to happen.

Usage examples:
- It can help with the game design - determining the odds of a combo may help you decide if such a combo should be considered an issue, or can be tolerated. For example, a combo that lets your opponent win during the first turn may be unfuriating in PvP games if it happens every 5th games, but wouldn't hurt if it happened once per thousands of games.
- It can help with balancing a card game - it's easier to estimate potential strength of card synergies if you're able to calculate the odds of them happening. The more likely the combo is, the weaker it should be to maintain the balance.
- It can improve AI algorithms - in some cases, AI may want to know what are the odds of particular combo to happen. For example, it can be used to estimate likeness that opponent will play some combo, or predict if spending resources to draw cards will be worth it.
- It can be used to detect suspicious data during its validation - calculated probabilites can be used to compare them with processed data collected from players to estimate if the random number generator is rigged or if the data is corrupted. For example, player community can use data trackers to collect the info from games and validate if developers can be trusted. Another example: a offline game can use it to validate if players from the top of leaderboard likely hacked and rigged the RNG. Keep in mind, that true randomness is unpredictable, meaning that data validation won't prove rigging, as it can only detect suspicious activites.

There are 3 variants of the algorithm. Every variant returns an array of probability sets, where each probability set corresponds one card combination (e.g., 1 card B and 1 card C by the 9th draw). In each probability set, you can find alias of the card combination, and probabilities of it happening during each turn. Keep in mind that probabilities use AND logic - all combinations that do not fulfill previously checked restriction will be cut out. Additionally, each variant returns an extra data array:
- Full table - it returns the table of possible sequences of each draw combination. The first dimension represents the number of the drawn cards. Other N dimensions represent the N different card groups. It's great for making charts, as it contains every possible data.
- Cropped table - similar to the full table, but the dimensions represeting card groups have bounds equal to the max expected value in corresponding card groups. For example, if the first card group contains 10 cards, but player only needs 2 of them, then the corresponding array will have legnth 3. The values not fitting the bounds are merged with corresponding edge cells of the array, which can be interpreted as a sufix sum.
- Optimized - it returns a one-dimensional table, containing only the data from the last needed card draw. It's the most performant.

The Unity project contains multiple sample tests to test out the algorithm's speed and compare results with other algorithm variants.

Algorithm DOESN'T support multithreading.
