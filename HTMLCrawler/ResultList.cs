namespace HTMLCrawler
{

    public class ResultList<T>
    {
    
        public delegate void Visitor(T item);

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

        public void Add(T value)
        {
            Node node = new Node(value);

            lock (_locker)
            {
                if (_head == null)
                {
                    _head = _tail = node;
                }
                else
                {
                    _tail!.Next = node;
                    _tail = node;
                }
                _count++;
            }
        }

        public int Count
        {
            get
            {
                lock (_locker)
                {
                    return _count;
                }
            }
        }



        public void ForEach(Visitor visit)
        {
            if (visit == null) return;

      
            Node? current = _head;
            while (current != null)
            {
                visit(current.Value);
                current = current.Next;
            }
        }


        public T[] ToArraySnapshot()
        {
            T[] result;

            lock (_locker)
            {
                result = new T[_count];
                Node? current = _head;
                int i = 0;

                while (current != null)
                {
                    result[i++] = current.Value;
                    current = current.Next;
                }
            }

            return result;
        }

    }
}
