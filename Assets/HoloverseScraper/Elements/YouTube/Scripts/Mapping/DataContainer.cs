using System;
using System.Collections;
using System.Collections.Generic;
using Midnight;

namespace Holoverse.Scraper
{
	public class DataContainer<T> : IList<T>
	{
		public List<Func<T, bool>> filters = new List<Func<T, bool>>();
		public string savePath = string.Empty;

		private List<T> _allList = new List<T>();

		public T this[int index]
		{
			get => _allList[index];
			set => _allList[index] = value;
		}

		public int Count => _allList.Count;

		public bool IsReadOnly => ((ICollection<T>)_allList).IsReadOnly;

		public DataContainer() { }

		public DataContainer(IEnumerable<T> items)
		{
			_allList.Clear();
			_allList.AddRange(items);
		}

		public void Add(T item)
		{
			foreach(Func<T, bool> filter in filters) {
				if(!filter(item)) { return; }
			}

			_allList.Add(item);
		}

		public void AddRange(IEnumerable<T> items)
		{
			_allList.AddRange(items);
		}

		public void Replace(IEnumerable<T> items)
		{
			Clear();
			AddRange(items);
		}

		public void Save()
		{
			JsonUtilities.SaveToDisk(_allList, new JsonUtilities.SaveToDiskParameters {
				filePath = savePath
			});
		}

		public void Clear()
		{
			_allList.Clear();
		}

		public bool Contains(T item)
		{
			return _allList.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_allList.CopyTo(array, arrayIndex);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _allList.GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return _allList.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			_allList.Insert(index, item);
		}

		public bool Remove(T item)
		{
			return _allList.Remove(item);
		}

		public void RemoveAt(int index)
		{
			_allList.RemoveAt(index);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _allList.GetEnumerator();
		}
	}
}