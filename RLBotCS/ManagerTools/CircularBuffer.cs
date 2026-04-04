namespace RLBotCS.ManagerTools;

public class CircularBuffer<T>
{
    private int _size = 0;
    private int _currentIndex = 0;
    private readonly T[] _buffer;

    public int Count => _buffer.Length;

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    public void AddLast(T item)
    {
        _buffer[_currentIndex] = item;
        _size = Math.Max(_currentIndex + 1, _size);
        _currentIndex = (_currentIndex + 1) % _buffer.Length;
    }

    public IEnumerable<T> Iter()
    {
        for (int i = 0; i < Math.Min(_buffer.Length, _size); i++)
            yield return _buffer[(i + _currentIndex) % _buffer.Length];
    }
}
