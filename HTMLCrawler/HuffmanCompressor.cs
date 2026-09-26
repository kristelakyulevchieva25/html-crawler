using System;
using System.IO;

namespace HTMLCrawler
{

    public class HuffmanCompressor
    {

        private readonly long[] _frequencies = new long[256];

        private readonly CharBuf?[] _huffmanCodes = new CharBuf[256];


        private byte _bitBuffer;
        private int _bitCount;


        public void Compress(HtmlNode root, Stream outputStream)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (outputStream == null) throw new ArgumentNullException(nameof(outputStream));


            string htmlText = root.RenderInline();

         
            Array.Clear(_frequencies, 0, _frequencies.Length);
            CountStringBytes(htmlText);

        
            HuffmanNode huffmanTree = BuildHuffmanTree();

            Array.Clear(_huffmanCodes, 0, _huffmanCodes.Length);
            BuildCodeTable(huffmanTree, new CharBuf());

            WriteHeader(outputStream);

            _bitBuffer = 0;
            _bitCount = 0;

            WriteStringBits(htmlText, outputStream);

            FlushBitBuffer(outputStream);
        }

        public HtmlNode Decompress(Stream inputStream)
        {
            if (inputStream == null) throw new ArgumentNullException(nameof(inputStream));

            Array.Clear(_frequencies, 0, _frequencies.Length);
            ReadHeader(inputStream);


            HuffmanNode huffmanTree = BuildHuffmanTree();


            long totalSymbols = 0;
            for (int i = 0; i < _frequencies.Length; i++)
            {
                totalSymbols += _frequencies[i];
            }

            using (var memoryStream = new MemoryStream())
            {
                ReadCompressedData(inputStream, memoryStream, huffmanTree, totalSymbols);
                memoryStream.Position = 0;

                var parser = new HtmlParser(memoryStream);
                HtmlNode root = parser.Parse();
                return root;
            }
        }



        private void CountStringBytes(string? s)
        {
            if (s == null) return;

            for (int i = 0; i < s.Length; i++)
            {
                int c = (int)s[i];
                if (c >= 0 && c <= 255)
                {
                    _frequencies[c]++;
                }
                else
                {
          
                    _frequencies[(byte)'?']++;
                }
            }
        }

        private HuffmanNode BuildHuffmanTree()
        {
            var pq = new MyPriorityQueue<HuffmanNode>();

    
            for (int i = 0; i < 256; i++)
            {
                if (_frequencies[i] > 0)
                {
                    pq.Enqueue(new HuffmanNode((byte)i, _frequencies[i]));
                }
            }

            if (pq.Count == 0)
            {
                throw new InvalidOperationException("No data to compress (all frequencies are zero).");
            }

        
            if (pq.Count == 1)
            {
                HuffmanNode leaf = pq.Dequeue();
                HuffmanNode dummy = new HuffmanNode(0, 0);
                HuffmanNode parent = new HuffmanNode(leaf, dummy);
                pq.Enqueue(parent);
            }

    
            while (pq.Count > 1)
            {
                HuffmanNode left = pq.Dequeue();
                HuffmanNode right = pq.Dequeue();
                HuffmanNode parent = new HuffmanNode(left, right);
                pq.Enqueue(parent);
            }

            return pq.Dequeue();
        }

        private void BuildCodeTable(HuffmanNode? node, CharBuf currentCode)
        {
            if (node == null) return;

            if (node.IsLeaf())
            {
             
                _huffmanCodes[node.Symbol] = currentCode.Clone();
                return;
            }

         
            CharBuf leftCode = currentCode.Clone();
            leftCode.Add('0');
            BuildCodeTable(node.Left, leftCode);

         
            CharBuf rightCode = currentCode.Clone();
            rightCode.Add('1');
            BuildCodeTable(node.Right, rightCode);
        }

        private void WriteHeader(Stream output)
        {
        
            for (int i = 0; i < _frequencies.Length; i++)
            {
                long freq = _frequencies[i];
                for (int b = 0; b < 8; b++)
                {
                 
                    output.WriteByte((byte)(freq & 0xFF));
                   
                    freq >>= 8;
                }
            }
        }

        private void WriteStringBits(string? s, Stream output)
        {
            if (s == null) return;

            for (int i = 0; i < s.Length; i++)
            {
                byte b = (byte)'?';
                int c = (int)s[i];

                if (c >= 0 && c <= 255)
                {
                    b = (byte)c;
                }

                CharBuf? code = _huffmanCodes[b];
                if (code == null)
                {
                    throw new IOException("Missing Huffman code for symbol.");
                }

                for (int j = 0; j < code.Length; j++)
                {
                    WriteBit(code.GetAt(j), output);
                }
            }
        }

        private void WriteBit(char bit, Stream output)
        {
            _bitBuffer = (byte)(_bitBuffer << 1);
            if (bit == '1')
            {
                _bitBuffer = (byte)(_bitBuffer | 1);
            }

            _bitCount++;

            if (_bitCount == 8)
            {
                output.WriteByte(_bitBuffer);
                _bitBuffer = 0;
                _bitCount = 0;
            }
        }

        private void FlushBitBuffer(Stream output)
        {
            if (_bitCount > 0)
            {
                _bitBuffer = (byte)(_bitBuffer << (8 - _bitCount));
                output.WriteByte(_bitBuffer);
                _bitBuffer = 0;
                _bitCount = 0;
            }
        }

        private void ReadHeader(Stream input)
        {
            for (int i = 0; i < 256; i++)
            {
                long freq = 0;
                for (int b = 0; b < 8; b++)
                {
                    int readByte = input.ReadByte();
                    if (readByte == -1) throw new IOException("Invalid header.");

                  
                    freq |= ((long)readByte << (b * 8));
                }
                _frequencies[i] = freq;
            }
        }

        private void ReadCompressedData(Stream input, Stream output, HuffmanNode root, long totalSymbols)
        {
            long symbolsRead = 0;
            HuffmanNode currentNode = root;

            int b;
            byte bitBuffer = 0;
            int bitCount = 0;

            while (symbolsRead < totalSymbols)
            {
           
                if (bitCount == 0)
                {
                    b = input.ReadByte();
                    if (b == -1)
                    {
                        if (symbolsRead < totalSymbols)
                        {
                            throw new IOException("Invalid Huffman data: file ended prematurely.");
                        }
                        break;
                    }

                    bitBuffer = (byte)b;
                    bitCount = 8;
                }

                bool isZeroBit = (bitBuffer & 0x80) == 0; 
                bitBuffer = (byte)(bitBuffer << 1);
                bitCount--;

                currentNode = isZeroBit ? currentNode.Left! : currentNode.Right!;
                if (currentNode == null)
                {
                    throw new IOException("Invalid Huffman data: corrupted tree path.");
                }

                if (currentNode.IsLeaf())
                {
                    output.WriteByte(currentNode.Symbol);
                    symbolsRead++;
                    currentNode = root;
                }
            }
        }
    }
}
