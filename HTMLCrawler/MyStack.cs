using System;

namespace HTMLCrawler
{

    public class MyStack<T>
    {
    
        private T[] _items;
        private int _count;

        public MyStack(int capacity = 16)
        {
            if (capacity < 1) capacity = 1;
            _items = new T[capacity];
            _count = 0;
        }


        public int Count
        {
            get { return _count; }
        }

        public void Push(T value)
        {
            if (_count == _items.Length)
            {
             
                T[] bigger = new T[_items.Length * 2];
                for (int i = 0; i < _count; i++)
                {
                    bigger[i] = _items[i];
                }
                _items = bigger;
            }

            _items[_count] = value;
            _count++;
        }

        public T Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Stack is empty.");
            }

            _count--;
            T val = _items[_count];
            _items[_count] = default(T);
            return val;
        }

        public T Peek()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Stack is empty.");
            }

            return _items[_count - 1];
        }

    }
}
