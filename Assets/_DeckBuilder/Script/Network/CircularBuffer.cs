class CircularBuffer<T>
{
    T[] buffer;
    int bufferSize;

    public CircularBuffer(int size)
    {
        bufferSize = size;
        buffer = new T[bufferSize];
    }

    public void Add(T item, int index)
    {
        buffer[index % bufferSize] = item;
    }

    public T Get(int index)
    {
        return buffer[index % bufferSize];
    }

    public void Clear()
    {
        buffer = new T[bufferSize];
    }
}