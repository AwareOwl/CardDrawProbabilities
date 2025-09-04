using System;

namespace DeckUtilities
{
    [Serializable]
    public class CardGroup
    {
        /// <summary>
        /// A name that will make it easier to identify the group in the inspector or in logs.
        /// </summary>
        public string _groupName;
        /// <summary>
        /// Defines how many cards from the given group exist.
        /// </summary>
        public uint _groupSize;

        public CardGroup(string groupName, uint groupSize)
        {
            _groupName = groupName;
            _groupSize = groupSize;
        }

        public CardGroup(CardGroup cardGroup)
        {
            _groupName = cardGroup._groupName;
            _groupSize = cardGroup._groupSize;
        }
    }
}