using System;

namespace HTMLCrawler
{

    internal sealed class CharBuf
    {
    
        private char[] _buffer = new char[64];  
        private int _length = 0;


        public int Length => _length;


        public void Add(char c)
        {
            EnsureCapacity(1);
            _buffer[_length++] = c;
        }


        public void Add(string s)
        {
            if (s == null) return;

            int addLen = s.Length;
            if (addLen == 0) return;

            EnsureCapacity(addLen);

            for (int i = 0; i < addLen; i++)
            {
                _buffer[_length + i] = s[i];
            }

            _length += addLen;
        }


        public void Clear()
        {
            _length = 0;
        }


        public override string ToString()
        {
            return new string(_buffer, 0, _length);
        }


        public char GetAt(int index)
        {
            
            return _buffer[index];
        }


        public CharBuf Clone()
        {
            var clone = new CharBuf();

           
            if (_length > clone._buffer.Length)
            {
                clone._buffer = new char[_length];
            }

          
            for (int i = 0; i < _length; i++)
            {
                clone._buffer[i] = _buffer[i];
            }

            clone._length = _length;
            return clone;
        }


        private void EnsureCapacity(int extra)
        {
            int need = _length + extra;
            if (need <= _buffer.Length) return;

            int newCap = _buffer.Length;
            while (newCap < need)
            {
                newCap *= 2;
            }

            char[] bigger = new char[newCap];
            for (int i = 0; i < _length; i++)
            {
                bigger[i] = _buffer[i];
            }
            _buffer = bigger;
        }
    }
}
