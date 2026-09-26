using System.Threading;

namespace HTMLCrawler
{

    public static class ThreadLabel
    {

        public static string Get()
        {
            int id = Thread.CurrentThread.ManagedThreadId;

       
            if (id < 10)
            {
                return "[T0" + id + "]";
            }

            return "[T" + id + "]";
        }
    }
}
