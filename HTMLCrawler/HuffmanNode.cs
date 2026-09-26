using System;

namespace HTMLCrawler
{

    public class HuffmanNode : IComparable<HuffmanNode>
    {
    
        public byte Symbol;       
        public long Frequency;    


        public HuffmanNode? Left;  
        public HuffmanNode? Right; 

        public HuffmanNode(byte symbol, long frequency)
        {
            Symbol = symbol;
            Frequency = frequency;
            Left = null;
            Right = null;
        }

        public HuffmanNode(HuffmanNode left, HuffmanNode right)
        {
            Left = left;
            Right = right;
            Frequency = left.Frequency + right.Frequency;
            Symbol = 0; 
        }


        public bool IsLeaf()
        {
            return Left == null && Right == null;
        }


        public int CompareTo(HuffmanNode? other)
        {
            if (other == null) return 1;

            if (Frequency < other.Frequency) return -1;
            if (Frequency > other.Frequency) return 1;

         
            if (Symbol < other.Symbol) return -1;
            if (Symbol > other.Symbol) return 1;

            return 0;
        }
    }
}
