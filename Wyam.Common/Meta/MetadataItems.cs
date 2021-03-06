using System;
using System.Collections;
using System.Collections.Generic;

namespace Wyam.Common.Meta
{
    public class MetadataItems : IList<MetadataItem>
    {
        private readonly List<MetadataItem> _items = new List<MetadataItem>();

        public IEnumerator<MetadataItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(string key, object value)
        {
            _items.Add(new MetadataItem(key, value));
        }

        public void Add(KeyValuePair<string, object> pair)
        {
            _items.Add(new MetadataItem(pair));
        }

        public void Add(string key, Func<string, IMetadata, object> value, bool cacheValue = false)
        {
            _items.Add(new MetadataItem(key, value, cacheValue));
        }

        public void Add(MetadataItem item)
        {
            _items.Add(item);
        }

        public void AddRange(IEnumerable<MetadataItem> items)
        {
            _items.AddRange(items);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(MetadataItem item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(MetadataItem[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public bool Remove(MetadataItem item)
        {
            return _items.Remove(item);
        }

        public int Count => _items.Count;

        public bool IsReadOnly => false;

        public int IndexOf(MetadataItem item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, MetadataItem item)
        {
            _items.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        public MetadataItem this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }
    }
}