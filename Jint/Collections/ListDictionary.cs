using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Jint.Runtime;

namespace Jint.Collections
{
    internal sealed class ListDictionary<TValue> : IEnumerable<KeyValuePair<Key, TValue>>
    {
        private DictionaryNode _head;
        private int _count;
        private bool _checkExistingKeys;

        public ListDictionary(Key key, TValue value, bool checkExistingKeys)
        {
            _checkExistingKeys = checkExistingKeys;
            _head = new DictionaryNode
            {
                Key = key, 
                Value = value
            };
            _count = 1;
        }

        public TValue this[Key key]
        {
            get
            {
                TryGetValue(key, out var value);
                return value;
            }
            set
            {
                DictionaryNode last = null;
                DictionaryNode node;
                var checkExistingKeys = _checkExistingKeys;
                for (node = _head; node != null; node = node.Next)
                {
                    var oldKey = node.Key;
                    if (checkExistingKeys && oldKey == key)
                    {
                        break;
                    }

                    last = node;
                }

                if (node != null)
                {
                    // Found it
                    node.Value = value;
                    return;
                }

                AddNode(key, value, last);
            }
        }

        public bool TryGetValue(Key key, out TValue value)
        {
            var node = _head;
            while (node != null)
            {
                if (node.Key == key)
                {
                    value = node.Value;
                    return true;
                }

                node = node.Next;
            }

            value = default;
            return false;
        }

        public int Count
        {
            
            get => _count;
        }

        public void Add(Key key, TValue value)
        {
            DictionaryNode last = null;
            DictionaryNode node;
            var checkExistingKeys = _checkExistingKeys;
            for (node = _head; node != null; node = node.Next)
            {
                var oldKey = node.Key;
                if (checkExistingKeys && oldKey == key)
                {
                    ExceptionHelper.ThrowArgumentException();
                }

                last = node;
            }

            AddNode(key, value, last);
        }

        private void AddNode(Key key, TValue value, DictionaryNode last)
        {
            var newNode = new DictionaryNode
            {
                Key = key,
                Value = value
            };
            if (_head is null)
            {
                _head = newNode;
            }
            else
            {
                last.Next = newNode;
            }
            _count++;
        }

        public void Clear()
        {
            _count = 0;
            _head = null;
        }

        public bool ContainsKey(Key key)
        {
            for (var node = _head; node != null; node = node.Next)
            {
                var oldKey = node.Key;
                if (oldKey == key)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool CheckExistingKeys
        {
            set => _checkExistingKeys = value;
        }

        public NodeEnumerator GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        IEnumerator<KeyValuePair<Key, TValue>> IEnumerable<KeyValuePair<Key, TValue>>.GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new NodeEnumerator(this);
        }

        public bool Remove(Key key)
        {
            DictionaryNode last = null;
            DictionaryNode node;
            for (node = _head; node != null; node = node.Next)
            {
                var oldKey = node.Key;
                if (oldKey == key)
                {
                    break;
                }

                last = node;
            }

            if (node == null)
            {
                return false;
            }

            if (node == _head)
            {
                _head = node.Next;
            }
            else
            {
                last.Next = node.Next;
            }

            _count--;
            return true;
        }

        internal struct NodeEnumerator : IEnumerator<KeyValuePair<Key, TValue>>
        {
            private readonly ListDictionary<TValue> _list;
            private DictionaryNode _current;
            private bool _start;

            public NodeEnumerator(ListDictionary<TValue> list)
            {
                _list = list;
                _start = true;
                _current = null;
            }

            public KeyValuePair<Key, TValue> Current => new KeyValuePair<Key, TValue>(_current.Key, _current.Value);

            public bool MoveNext()
            {
                if (_start)
                {
                    _current = _list._head;
                    _start = false;
                }
                else if (_current != null)
                {
                    _current = _current.Next;
                }

                return _current != null;
            }

            void IEnumerator.Reset()
            {
                _start = true;
                _current = null;
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current => _current;
        }

        internal class DictionaryNode
        {
            public Key Key;
            public TValue Value;
            public DictionaryNode Next;
        }
    }
}