using System.Collections.Generic;
using System.Windows.Input;

namespace CPvC
{
    /// <summary>
    /// Encapsulates a mapping of Windows keys to Amstrad CPC keys.
    /// </summary>
    public class KeyboardMapping
    {
        private Dictionary<Key, byte> _keyMap;

        public KeyboardMapping()
        {
            _keyMap = new Dictionary<Key, byte>();
        }

        public void Map(Key key, byte cpcKey)
        {
            _keyMap[key] = cpcKey;
        }

        public byte? GetKey(Key key)
        {
            if (_keyMap.ContainsKey(key))
            {
                return _keyMap[key];
            }

            return null;
        }
    }
}
