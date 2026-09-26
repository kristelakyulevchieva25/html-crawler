using System;
using System.Threading;

namespace HTMLCrawler
{

    public class WorkQueue<T>
    {
 
        private class Node
        {
            public T Value;
            public Node? Next;

            public Node(T value)
            {
                Value = value;
                Next = null;
            }
        }

   
        private Node? _head;
        private Node? _tail;


        private int _count;

        private readonly object _locker = new object();


        public void Enqueue(T value)
        {

            var node = new Node(value);

            lock (_locker)
            {
                if (_tail == null)
                {
              
                    _head = _tail = node;
                }
                else
                {
                    _tail.Next = node;
                    _tail = node;
                }

                _count++;

                Monitor.Pulse(_locker);
            }
        }


        public bool TryDequeue(out T value)
        {

            lock (_locker)
            {
                while (_count == 0)
                {
                   
                    if (!Monitor.Wait(_locker, 100))
                    {
                   
                        value = default(T)!;
                        return false;
                    }
                }

          
                Node node = _head!;
                _head = node.Next;

                if (_head == null)
                {
                    _tail = null;
                }

                _count--;

                value = node.Value;
                return true;
            }
        }
    }
}
