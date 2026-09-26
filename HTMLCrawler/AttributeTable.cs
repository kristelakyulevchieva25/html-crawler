using System;

namespace HTMLCrawler
{

    public class AttributeTable
    {

        private struct Attr
        {
            public string KeyLower;  
            public string Value;    
            public char Quote;       
        }

        
        private Attr[] _items = new Attr[8];
        private int _count = 0;

      

        public AttributeTable Clone()
        {
            var clone = new AttributeTable();

            if (_count == 0)
                return clone;

            
            if (clone._items.Length != _items.Length)
            {
                clone._items = new Attr[_items.Length];
            }

            clone._count = _count;

            for (int i = 0; i < _count; i++)
            {
                clone._items[i] = _items[i];
            }

            return clone;
        }

       
        public void Add(string key, string value, char quote)
        {
            if (key == null) key = "";
            if (value == null) value = "";

            char q = (quote == '"' || quote == '\'') ? quote : '"';
            string k = ToLowerAscii(key);

            int idx = FindIndex(k);
            if (idx != -1)
            {
                
                _items[idx].Value = value;
                _items[idx].Quote = q;
                return;
            }

            EnsureCapacity();
            _items[_count].KeyLower = k;
            _items[_count].Value = value;
            _items[_count].Quote = q;
            _count++;
        }

  
        public bool TryGet(string key, out string value)
        {
            int idx = FindIndex(ToLowerAscii(key ?? ""));
            if (idx != -1)
            {
                value = _items[idx].Value;
                return true;
            }

            value = "";
            return false;
        }

      
        public bool ValueEquals(string key, string expected)
        {
            int idx = FindIndex(ToLowerAscii(key ?? ""));
            if (idx == -1) return false;
            return StringEquals(_items[idx].Value, expected ?? "");
        }


        public string BuildAttributeString()
        {
            if (_count == 0) return "";

            var buf = new CharBuf();

            for (int i = 0; i < _count; i++)
            {
                buf.Add(' ');
                buf.Add(_items[i].KeyLower);
                buf.Add('=');
                buf.Add(_items[i].Quote);
                buf.Add(_items[i].Value);
                buf.Add(_items[i].Quote);
            }

            return buf.ToString();
        }


        private void EnsureCapacity()
        {
            if (_count < _items.Length) return;

            Attr[] bigger = new Attr[_items.Length * 2];
            for (int i = 0; i < _items.Length; i++)
            {
                bigger[i] = _items[i];
            }
            _items = bigger;
        }


        private int FindIndex(string keyLower)
        {
            for (int i = 0; i < _count; i++)
            {
                if (StringEquals(_items[i].KeyLower, keyLower))
                    return i;
            }
            return -1;
        }


        private static string ToLowerAscii(string s)
        {
            if (s == null) return "";

            var buf = new CharBuf();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= 'A' && c <= 'Z')
                {
                    c = (char)(c - 'A' + 'a');
                }
                buf.Add(c);
            }
            return buf.ToString();
        }


        private static bool StringEquals(string a, string b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
