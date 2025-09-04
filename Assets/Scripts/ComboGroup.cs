using System;

namespace DeckUtilities
{
    [Serializable]
    public class ComboGroup
    {
        /// <summary>
        /// A name that will make it easier to identify the group in the inspector or in logs.
        /// </summary>
        public string _groupName;
        /// <summary>
        /// If false, algorithm will ignore it. Useful during testing from the inspector level.
        /// </summary>
        public bool _enabled;
        /// <summary>
        /// Defines how much cards from a group at the same index is required.
        /// </summary>
        public uint[] _requiredCount;
        /// <summary>
        /// Defines the draw number at which the combo has to be played.
        /// </summary>
        public uint _targetDraw;

        public ComboGroup(string groupName, uint[] requiredCount)
        {
            _groupName = groupName;
            _enabled = true;
            _requiredCount = requiredCount;
            _targetDraw = 2;
        }

        public ComboGroup(ComboGroup comboGroup)
        {
            _groupName = comboGroup._groupName;
            _enabled = comboGroup._enabled;
            _requiredCount = new uint[comboGroup._requiredCount.Length];
            for (int i = 0; i < _requiredCount.Length; i++)
            {
                _requiredCount [i] = comboGroup._requiredCount [i];
            }
            _targetDraw = comboGroup._targetDraw;
        }
    }
}