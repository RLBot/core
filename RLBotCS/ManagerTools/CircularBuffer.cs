namespace RLBotCS.ManagerTools;

public class CircularBuffer<T>
{
    private int _startIndex;
    private int _currentIndex;
    private readonly T[] _buffer;

    public int Count => (_currentIndex - _startIndex + _buffer.Length) % _buffer.Length;

    public CircularBuffer(int capacity)
    {
        _buffer = new T[capacity];
    }

    public void AddLast(T item)
    {
        _buffer[_currentIndex] = item;

        // continuously overwrite the oldest item once full
        _currentIndex = (_currentIndex + 1) % _buffer.Length;
        if (_currentIndex == _startIndex)
            _startIndex = (_startIndex + 1) % _buffer.Length;
    }

    public IEnumerable<T> Iter()
    {
        for (int i = _startIndex; i != _currentIndex; i = (i + 1) % _buffer.Length)
            yield return _buffer[i];
    }
}
