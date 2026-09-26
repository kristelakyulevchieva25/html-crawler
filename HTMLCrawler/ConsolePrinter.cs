using System;

namespace HTMLCrawler
{

    public static class ConsolePrinter
    {
     
        private static readonly object _locker = new object();


        public static void Line(string s)
        {
            if (s == null) s = "";

            lock (_locker)
            {
                Console.WriteLine(s);
            }
        }


        public static void Raw(string s)
        {
            if (s == null) s = "";

            lock (_locker)
            {
                Console.Write(s);
            }
        }

    }
}
