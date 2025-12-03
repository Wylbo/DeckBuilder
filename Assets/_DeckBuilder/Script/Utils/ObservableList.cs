using System;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class ObservableList<T> : IList<T>, IList, IReadOnlyList<T>
{
    private readonly List<T> items;
    private object syncRoot;

    public event Action<T> OnItemAdded;
    public event Action<T> OnItemRemoved;

    public ObservableList()
    {
        items = new List<T>();
    }

    public ObservableList(IEnumerable<T> collection)
    {
        items = collection != null ? new List<T>(collection) : new List<T>();
    }

    public T this[int index]
    {
        get => items[index];
        set
        {
            T existing = items[index];

            if (EqualityComparer<T>.Default.Equals(existing, value))
            {
                items[index] = value;
                return;
            }

            items[index] = value;
            OnItemRemoved?.Invoke(existing);
            OnItemAdded?.Invoke(value);
        }
    }

    public int Count => items.Count;

    public bool IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot
    {
        get
        {
            if (syncRoot == null)
            {
                ICollection collection = items;
                syncRoot = collection.SyncRoot;
            }

            return syncRoot;
        }
    }

    bool IList.IsFixedSize => false;

    public void Add(T item)
    {
        items.Add(item);
        OnItemAdded?.Invoke(item);
    }

    public void Insert(int index, T item)
    {
        items.Insert(index, item);
        OnItemAdded?.Invoke(item);
    }

    public bool Remove(T item)
    {
        bool removed = items.Remove(item);
        if (removed)
        {
            OnItemRemoved?.Invoke(item);
        }

        return removed;
    }

    public void RemoveAt(int index)
    {
        T item = items[index];
        items.RemoveAt(index);
        OnItemRemoved?.Invoke(item);
    }

    public void Clear()
    {
        if (items.Count == 0)
        {
            return;
        }

        T[] removedItems = items.ToArray();
        items.Clear();

        foreach (T item in removedItems)
        {
            OnItemRemoved?.Invoke(item);
        }
    }

    public bool Contains(T item)
    {
        return items.Contains(item);
    }

    public int IndexOf(T item)
    {
        return items.IndexOf(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        items.CopyTo(array, arrayIndex);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ((ICollection)items).CopyTo(array, index);
    }

    object IList.this[int index]
    {
        get => this[index];
        set
        {
            VerifyValueType(value);
            this[index] = (T)value;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    int IList.Add(object value)
    {
        VerifyValueType(value);
        Add((T)value);
        return Count - 1;
    }

    bool IList.Contains(object value)
    {
        return IsCompatibleObject(value) && Contains((T)value);
    }

    int IList.IndexOf(object value)
    {
        return IsCompatibleObject(value) ? IndexOf((T)value) : -1;
    }

    void IList.Insert(int index, object value)
    {
        VerifyValueType(value);
        Insert(index, (T)value);
    }

    void IList.Remove(object value)
    {
        if (IsCompatibleObject(value))
        {
            Remove((T)value);
        }
    }

    private static void VerifyValueType(object value)
    {
        if (!IsCompatibleObject(value))
        {
            throw new ArgumentException($"Value is of incorrect type. Expected {typeof(T)}.");
        }
    }

    private static bool IsCompatibleObject(object value)
    {
        if (value is T)
        {
            return true;
        }

        if (value == null)
        {
            return default(T) == null;
        }

        return false;
    }
}
