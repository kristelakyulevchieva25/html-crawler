using System;

namespace HTMLCrawler
{

    public class MyPriorityQueue<T> where T : IComparable<T>
    {
 
        private T[] _heap;

        private int _count;

        public MyPriorityQueue(int initialCapacity = 16)
        {
            if (initialCapacity < 1) initialCapacity = 1;
      
            _heap = new T[initialCapacity + 1];
            _count = 0;
        }

        public int Count
        {
            get { return _count; }
        }

        public void Enqueue(T item)
        {
            EnsureCapacity();


            _count++;
            _heap[_count] = item;

       
            SiftUp(_count);
        }


        public T Dequeue()
        {
            if (_count == 0)
            {
        
                throw new InvalidOperationException("Priority queue is empty.");
            }

        
            T minItem = _heap[1];

          
            _heap[1] = _heap[_count];
            _heap[_count] = default(T); 
            _count--;


            if (_count > 0)
            {
                SiftDown(1);
            }

            return minItem;
        }

        private void SiftUp(int index)
        {
            int parentIndex = index / 2;

       
            while (index > 1 && IsLess(index, parentIndex))
            {
                Swap(index, parentIndex);
                index = parentIndex;
                parentIndex = index / 2;
            }
        }


        private void SiftDown(int index)
        {
            while (true)
            {
                int leftChild = index * 2;
                int rightChild = index * 2 + 1;
                int smallest = index; 

                if (leftChild <= _count && IsLess(leftChild, smallest))
                {
                    smallest = leftChild;
                }

          
                if (rightChild <= _count && IsLess(rightChild, smallest))
                {
                    smallest = rightChild;
                }

          
                if (smallest == index)
                {
                    break;
                }

             
                Swap(index, smallest);
                index = smallest;
            }
        }


        private bool IsLess(int indexA, int indexB)
        {
            return _heap[indexA].CompareTo(_heap[indexB]) < 0;
        }


        private void Swap(int indexA, int indexB)
        {
            T temp = _heap[indexA];
            _heap[indexA] = _heap[indexB];
            _heap[indexB] = temp;
        }

        private void EnsureCapacity()
        {
          
            if (_count < _heap.Length - 1) return;

            T[] biggerHeap = new T[_heap.Length * 2];
            for (int i = 1; i <= _count; i++)
            {
                biggerHeap[i] = _heap[i];
            }
            _heap = biggerHeap;
        }
    }
}
