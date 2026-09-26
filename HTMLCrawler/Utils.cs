using System;

namespace HTMLCrawler
{

    public static class Utils
    {

        public static bool EqualsIgnoreCase(string? a, string? b)
        {

            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                char ca = a[i];
                char cb = b[i];

                if (ca >= 'A' && ca <= 'Z') ca = (char)(ca - 'A' + 'a');
                if (cb >= 'A' && cb <= 'Z') cb = (char)(cb - 'A' + 'a');

                if (ca != cb) return false;
            }

            return true;
        }


        public static string ToLowerAscii(string? s)
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


        public static string NormalizeTag(string tagName)
        {

            return ToLowerAscii(tagName);
        }

 
        public static string TrimManual(string? s)
        {
          
            if (s == null || s.Length == 0) return "";

            int a = 0;
            int b = s.Length - 1;

            while (a <= b && IsWs(s[a])) a++;
            while (b >= a && IsWs(s[b])) b--;

            if (b < a) return "";

            var buf = new CharBuf();
            for (int i = a; i <= b; i++)
            {
                buf.Add(s[i]);
            }

            return buf.ToString();
        }


        public static bool IsNullOrEmpty(string? s)
        {
       
            return s == null || s.Length == 0;
        }

        public static bool IsWs(char c)
        {
   
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        public static string SubstringSafe(string? s, int start, int length)
        {

            if (s == null) return "";

            if (start < 0) start = 0;
            if (length < 0) length = 0;
            if (start > s.Length) start = s.Length;
            if (start + length > s.Length) length = s.Length - start;

            var buf = new CharBuf();
            for (int i = 0; i < length; i++)
            {
                buf.Add(s[start + i]);
            }

            return buf.ToString();
        }

        public static string StripOuterQuotes(string? s)
        {

            if (s == null) return "";
            if (s.Length >= 2)
            {
                char f = s[0];
                char l = s[s.Length - 1];

                if ((f == '"' && l == '"') ||
                    (f == '\'' && l == '\''))
                {
                    var buf = new CharBuf();
                    for (int i = 1; i < s.Length - 1; i++)
                    {
                        buf.Add(s[i]);
                    }
                    return buf.ToString();
                }
            }

            return s;
        }
    }
}
