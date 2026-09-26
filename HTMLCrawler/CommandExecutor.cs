using System;
using System.IO;

namespace HTMLCrawler
{

    public class CommandExecutor
    {
     
        private HtmlNode _root;
        private string _baseFolder;

        private const int DEFAULT_THREADS = 4;

        public CommandExecutor(HtmlNode root, string baseFolder)
        {
            _root = root;
            _baseFolder = baseFolder ?? "";
        }

        public void Execute(string command)
        {
            if (command == null)
            {
                ConsolePrinter.Line("Empty command.");
                return;
            }

            string input = TrimManual(command);
            if (input.Length == 0)
            {
                ConsolePrinter.Line("Empty command.");
                return;
            }

            string action, rest;
            ParseCommandLine(input, out action, out rest);

            if (EqualsIgnoreCase(action, "PRINT"))
                HandlePrint(rest);
            else if (EqualsIgnoreCase(action, "PRINTP"))
                HandlePrintParallel(rest);
            else if (EqualsIgnoreCase(action, "SET"))
                HandleSet(rest);
            else if (EqualsIgnoreCase(action, "COPY"))
                HandleCopy(rest);
            else if (EqualsIgnoreCase(action, "SAVE"))
                HandleSave(rest);
            else if (EqualsIgnoreCase(action, "LOAD"))
                HandleLoad(rest);
            else if (EqualsIgnoreCase(action, "EXIT"))
                Environment.Exit(0);
            else
            {
                ConsolePrinter.Line("Unknown command: " + action);
                ConsolePrinter.Line("Allowed: PRINT | PRINTP | SET | COPY | SAVE | LOAD | EXIT");
            }
        }


        private void HandlePrint(string rest)
        {
            if (rest.Length == 0)
            {
                ConsolePrinter.Line("Missing path argument.");
                return;
            }

            string path = StripOuterQuotes(TrimManual(rest));
            if (!StartsWithDoubleSlash(path))
            {
                ConsolePrinter.Line("Invalid path. XPath must start with //");
                return;
            }

  
            if (path.Length == 2 && path[0] == '/' && path[1] == '/')
            {
                _root.Print();
                ConsolePrinter.Line("--- End of search ---");
                return;
            }

            var searcher = new HtmlSearcher(_root);
            searcher.FindAndPrint(path);
        }

        private void HandlePrintParallel(string rest)
        {
            if (rest.Length == 0)
            {
                ConsolePrinter.Line("Missing path argument.");
                return;
            }

            int i = 0;
            while (i < rest.Length && IsWs(rest[i])) i++;

         
            string path;
            if (i < rest.Length && (rest[i] == '"' || rest[i] == '\''))
            {
                char q = rest[i++];
                var buf = new CharBuf();
                while (i < rest.Length && rest[i] != q)
                {
                    buf.Add(rest[i]);
                    i++;
                }
                if (i < rest.Length && rest[i] == q) i++;
                path = TrimManual(buf.ToString());
            }
            else
            {
                var buf = new CharBuf();
                while (i < rest.Length && !IsWs(rest[i]))
                {
                    buf.Add(rest[i]);
                    i++;
                }
                path = TrimManual(buf.ToString());
            }

            if (!StartsWithDoubleSlash(path))
            {
                ConsolePrinter.Line("Invalid path. XPath must start with //");
                return;
            }

            
            while (i < rest.Length && IsWs(rest[i])) i++;

            int threads = DEFAULT_THREADS;
            if (i < rest.Length)
            {
                int sign = 1;
                if (rest[i] == '+') { sign = 1; i++; }
                else if (rest[i] == '-') { sign = -1; i++; }

                int num = 0;
                bool any = false;
                while (i < rest.Length && rest[i] >= '0' && rest[i] <= '9')
                {
                    num = num * 10 + (rest[i] - '0');
                    any = true;
                    i++;
                }
                if (any) threads = sign * num;
            }

            if (threads < 1) threads = 1;

            if (path.Length == 2 && path[0] == '/' && path[1] == '/')
            {
                _root.Print();
                ConsolePrinter.Line("--- End of search ---");
                return;
            }

            var searcher = new HtmlSearcher(_root);
            searcher.FindAndPrintParallel(path, threads);
        }

        private void HandleSet(string rest)
        {
            string path, value;
            if (!ParseTwoQuotedArgs(rest, out path, out value))
            {
                ConsolePrinter.Line("Usage: SET \"//path\" \"text or <html>\"");
                return;
            }

            if (!StartsWithDoubleSlash(path))
            {
                ConsolePrinter.Line("Invalid path. XPath must start with //");
                return;
            }

            var searcher = new HtmlSearcher(_root);
            HtmlNode[] matches;
            int count;
            searcher.CollectMatches(path, out matches, out count);

            if (count == 0)
            {
                ConsolePrinter.Line("No matches for: " + path);
                return;
            }

            bool isHtml = LooksLikeHtmlFragment(value);

            
            if (!isHtml)
            {
                for (int i = 0; i < count; i++)
                    matches[i].ReplaceInnerWithText(value);

                ConsolePrinter.Line("SET OK (text). Affected nodes: " + count);
                return;
            }

            
            HtmlNode? fragmentFirst = ParseFragmentFirstChild(value);
            if (fragmentFirst == null)
            {
               
                for (int i = 0; i < count; i++)
                    matches[i].ReplaceInnerWithText(value);

                ConsolePrinter.Line("SET OK (text fallback). Affected nodes: " + count);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                HtmlNode cloneChain = fragmentFirst.DeepClone();
                matches[i].ReplaceInnerWithChildren(cloneChain);
            }

            ConsolePrinter.Line("SET OK (html). Affected nodes: " + count);
        }

        private void HandleCopy(string rest)
        {
            string sourcePath, targetPath;
            if (!ParseTwoQuotedArgs(rest, out sourcePath, out targetPath))
            {
                ConsolePrinter.Line("Usage: COPY \"//source-path\" \"//target-path\"");
                return;
            }

            if (!StartsWithDoubleSlash(sourcePath) || !StartsWithDoubleSlash(targetPath))
            {
                ConsolePrinter.Line("Invalid path. Both paths must start with //");
                return;
            }

            var searcher = new HtmlSearcher(_root);

           
            HtmlNode[] sourceMatches;
            int sourceCount;
            searcher.CollectMatches(sourcePath, out sourceMatches, out sourceCount);

            if (sourceCount == 0)
            {
                ConsolePrinter.Line("Source node not found: " + sourcePath);
                return;
            }

            if (sourceCount > 1)
            {
                ConsolePrinter.Line("Ambiguous source: path matches " + sourceCount + " nodes.");
                return;
            }

            HtmlNode sourceNode = sourceMatches[0];

           
            HtmlNode[] targetMatches;
            int targetCount;
            searcher.CollectMatches(targetPath, out targetMatches, out targetCount);

            if (targetCount == 0)
            {
                ConsolePrinter.Line("No target nodes found: " + targetPath);
                return;
            }

           
            if (sourceNode.Child == null && !Utils.IsNullOrEmpty(sourceNode.InnerText))
            {
                string srcText = sourceNode.InnerText!;
                for (int i = 0; i < targetCount; i++)
                {
                    targetMatches[i].ReplaceInnerWithText(srcText);
                }

                ConsolePrinter.Line("COPY OK (text). Affected nodes: " + targetCount);
                return;
            }

            
            if (sourceNode.Child == null)
            {
                for (int i = 0; i < targetCount; i++)
                {
                    targetMatches[i].ReplaceInnerWithChildren(null);
                }

                ConsolePrinter.Line("COPY OK (empty source). Affected nodes: " + targetCount);
                return;
            }

           
            HtmlNode sourceChildren = sourceNode.Child!;
            for (int i = 0; i < targetCount; i++)
            {
                HtmlNode? clonedFirstChild = BuildShallowChildrenChain(sourceChildren);
                targetMatches[i].ReplaceInnerWithChildren(clonedFirstChild);
            }

            ConsolePrinter.Line("COPY OK (shallow subtree copy). Affected nodes: " + targetCount);
        }

        private HtmlNode? BuildShallowChildrenChain(HtmlNode sourceFirstChild)
        {
            HtmlNode container = new HtmlNode("root");

            HtmlNode? cur = sourceFirstChild;
            while (cur != null)
            {
                HtmlNode clone = cur.ShallowClone();
                container.AddChild(clone);
                cur = cur.Next;
            }

            return container.Child;
        }


        private void HandleSave(string rest)
        {
            string path = StripOuterQuotes(TrimManual(rest));
            if (path.Length == 0)
            {
                ConsolePrinter.Line("Usage: SAVE \"archive.huff\"");
                return;
            }

          
            string finalPath = ContainsSlash(path) ? path : MakeFullPath(_baseFolder, path);

            try
            {
                var compressor = new HuffmanCompressor();
                using (var fileStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write))
                {
                   
                    compressor.Compress(_root, fileStream);

                 
                    SaveImages(fileStream);
                }

                ConsolePrinter.Line("Document saved to: " + finalPath);
            }
            catch (Exception ex)
            {
                ConsolePrinter.Line("Error saving file: " + ex.Message);
            }
        }

        private void HandleLoad(string rest)
        {
            string path = StripOuterQuotes(TrimManual(rest));
            if (path.Length == 0)
            {
                ConsolePrinter.Line("Usage: LOAD \"archive.huff\"");
                return;
            }

            string finalPath = ContainsSlash(path) ? path : MakeFullPath(_baseFolder, path);

            if (!File.Exists(finalPath))
            {
                ConsolePrinter.Line("File not found: " + finalPath);
                return;
            }

            try
            {
                var compressor = new HuffmanCompressor();
                HtmlNode newRoot;

                using (var fileStream = new FileStream(finalPath, FileMode.Open, FileAccess.Read))
                {
                
                    newRoot = compressor.Decompress(fileStream);

                   
                    _baseFolder = ExtractFolder(finalPath);

                  
                    LoadImages(fileStream);
                }

                _root = newRoot;
                ConsolePrinter.Line("Successfully loaded document from: " + finalPath);
                ConsolePrinter.Line("New document is now active.");
            }
            catch (Exception ex)
            {
                ConsolePrinter.Line("Error loading file: " + ex.Message);
            }
        }


        private void CollectImageSources(HtmlNode node, ref string[] names, ref int count)
        {
            if (node == null) return;

            if (Utils.EqualsIgnoreCase(node.TagName, "img"))
            {
                string src;
                if (node.Attributes.TryGet("src", out src))
                {
                    if (!StringListContains(names, count, src))
                    {
                        EnsureStringArray(ref names, count + 1);
                        names[count++] = src;
                    }
                }
            }

            HtmlNode? ch = node.Child;
            while (ch != null)
            {
                CollectImageSources(ch, ref names, ref count);
                ch = ch.Next;
            }
        }


        private void SaveImages(Stream archive)
        {
            string[] names = new string[8];
            int count = 0;
            CollectImageSources(_root, ref names, ref count);

          
            WriteInt32(archive, count);

            byte[] buf = new byte[4096];

            for (int i = 0; i < count; i++)
            {
                string name = names[i];

                byte[] nameBytes = MakeAsciiBytes(name);
                WriteInt32(archive, nameBytes.Length);
                archive.Write(nameBytes, 0, nameBytes.Length);

                string fullPath = MakeFullPath(_baseFolder, name);
                if (!File.Exists(fullPath))
                {
           
                    WriteInt64(archive, 0);
                    continue;
                }

                using (var img = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                {
                    long len = img.Length;
                    WriteInt64(archive, len);

                    long left = len;
                    while (left > 0)
                    {
                        int toRead = (left > buf.Length) ? buf.Length : (int)left;
                        int read = img.Read(buf, 0, toRead);
                        if (read <= 0) break;
                        archive.Write(buf, 0, read);
                        left -= read;
                    }
                }
            }
        }


        private void LoadImages(Stream archive)
        {
            int count = ReadInt32(archive);
            if (count <= 0) return;

            byte[] buf = new byte[4096];

            for (int i = 0; i < count; i++)
            {
                int nameLen = ReadInt32(archive);
                if (nameLen <= 0)
                {
                    long dummyLen = ReadInt64(archive);
                    continue;
                }

                string name = ReadAsciiString(archive, nameLen);
                long imgLen = ReadInt64(archive);
                if (imgLen <= 0) continue;

                string fullPath = MakeFullPath(_baseFolder, name);

                using (var img = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    long left = imgLen;
                    while (left > 0)
                    {
                        int toRead = (left > buf.Length) ? buf.Length : (int)left;
                        int read = archive.Read(buf, 0, toRead);
                        if (read <= 0) break;
                        img.Write(buf, 0, read);
                        left -= read;
                    }
                }
            }
        }



        private void EnsureStringArray(ref string[] a, int need)
        {
            if (a == null || a.Length == 0) a = new string[8];
            if (need <= a.Length) return;

            string[] b = new string[a.Length * 2];
            for (int i = 0; i < a.Length; i++) b[i] = a[i];
            a = b;
        }

        private bool StringListContains(string[] a, int n, string s)
        {
            for (int i = 0; i < n; i++)
            {
                if (Utils.EqualsIgnoreCase(a[i], s)) return true;
            }
            return false;
        }




        private void WriteInt32(Stream s, int value)
        {
        
            s.WriteByte((byte)(value & 0xFF));
            s.WriteByte((byte)((value >> 8) & 0xFF));
            s.WriteByte((byte)((value >> 16) & 0xFF));
            s.WriteByte((byte)((value >> 24) & 0xFF));
        }

        private void WriteInt64(Stream s, long value)
        {
            for (int i = 0; i < 8; i++)
            {
                s.WriteByte((byte)(value & 0xFF));
                value >>= 8;
            }
        }



        private int ReadInt32(Stream s)
        {
            int res = 0;
            for (int i = 0; i < 4; i++)
            {
                int b = s.ReadByte();
                if (b == -1)
                {

                    throw new EndOfStreamException("Unexpected end of stream while reading Int32.");
                }
                res |= (b << (i * 8));
            }
            return res;
        }

        private long ReadInt64(Stream s)
        {
            long res = 0;
            for (int i = 0; i < 8; i++)
            {
         
                long b = s.ReadByte();
                if (b == -1)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading Int64.");
                }
                res |= (b << (i * 8));
            }
            return res;
        }

        private string ReadAsciiString(Stream s, int len)
        {
       
            var buf = new CharBuf();

            for (int i = 0; i < len; i++)
            {
          
                int b = s.ReadByte();

          
                if (b == -1)
                {
                    throw new EndOfStreamException("Unexpected end of stream while reading string.");
                }

               
                buf.Add((char)b);
            }

            return buf.ToString();
        }

        private string MakeFullPath(string folder, string name)
        {
            if (folder == null) folder = "";
            if (name == null) name = "";

            if (folder.Length == 0) return name;

            char last = folder[folder.Length - 1];
            bool hasSlash = (last == '\\' || last == '/');

            var buf = new CharBuf();
            buf.Add(folder);
            if (!hasSlash) buf.Add("\\");
            buf.Add(name);
            return buf.ToString();
        }

        private string ExtractFolder(string path)
        {
            if (path == null) return "";
            int lastSep = -1;

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
                if (c == '\\' || c == '/') lastSep = i;
            }

            if (lastSep <= 0) return "";

            var buf = new CharBuf();
            for (int i = 0; i < lastSep; i++) buf.Add(path[i]);
            return buf.ToString();
        }

        private byte[] MakeAsciiBytes(string s)
        {
            if (s == null) s = "";
            byte[] a = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                int v = s[i];
                if (v < 0) v = 0;
                if (v > 255) v = (int)'?';
                a[i] = (byte)v;
            }
            return a;
        }

        private bool ContainsSlash(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' || c == '/') return true;
            }
            return false;
        }


        private bool ParseTwoQuotedArgs(string rest, out string first, out string second)
        {
            first = "";
            second = "";
            int i = 0;

            while (i < rest.Length && IsWs(rest[i])) i++;
            if (i >= rest.Length) return false;
            if (!(rest[i] == '"' || rest[i] == '\'')) return false;

            char q1 = rest[i++];
            var b1 = new CharBuf();
            while (i < rest.Length && rest[i] != q1) { b1.Add(rest[i]); i++; }
            if (i >= rest.Length) return false;
            i++;

            first = TrimManual(b1.ToString());

            while (i < rest.Length && IsWs(rest[i])) i++;
            if (i >= rest.Length) return false;
            if (!(rest[i] == '"' || rest[i] == '\'')) return false;

            char q2 = rest[i++];
            var b2 = new CharBuf();
            while (i < rest.Length && rest[i] != q2) { b2.Add(rest[i]); i++; }
            if (i >= rest.Length) return false;
            i++;

            second = b2.ToString();
            return true;
        }

       
        private bool LooksLikeHtmlFragment(string s)
        {
            int iLt = -1, iGt = -1;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '<' && iLt == -1) iLt = i;
                if (c == '>' && iGt == -1) iGt = i;
            }
            return (iLt != -1 && iGt != -1 && iLt < iGt);
        }


        private HtmlNode? ParseFragmentFirstChild(string html)
        {
            byte[] bytes = MakeAsciiBytes(html);
            using (var ms = new MemoryStream(bytes))
            {
                var parser = new HtmlParser(ms);
                HtmlNode parsed = parser.Parse();
                if (parsed == null) return null;

                if (Utils.EqualsIgnoreCase(parsed.TagName, "root") && parsed.Child == null)
                    return null;

                return parsed;
            }
        }

        private static bool IsWs(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        private static string TrimManual(string s)
        {
            int a = 0, b = s.Length - 1;
            while (a <= b && IsWs(s[a])) a++;
            while (b >= a && IsWs(s[b])) b--;
            if (b < a) return "";

            var buf = new CharBuf();
            for (int j = a; j <= b; j++) buf.Add(s[j]);
            return buf.ToString();
        }

        private static void ParseCommandLine(string input, out string action, out string rest)
        {
            var a = new CharBuf();
            var r = new CharBuf();
            int i = 0;

            while (i < input.Length && IsWs(input[i])) i++;
            while (i < input.Length && !IsWs(input[i])) a.Add(input[i++]);
            while (i < input.Length && IsWs(input[i])) i++;
            while (i < input.Length) r.Add(input[i++]);

            action = a.ToString();
            rest = r.ToString();
        }

        private static string StripOuterQuotes(string s)
        {
            if (s.Length >= 2)
            {
                char f = s[0], l = s[s.Length - 1];
                if ((f == '"' && l == '"') || (f == '\'' && l == '\''))
                {
                    var buf = new CharBuf();
                    for (int i = 1; i < s.Length - 1; i++) buf.Add(s[i]);
                    return buf.ToString();
                }
            }
            return s;
        }

        private static bool StartsWithDoubleSlash(string s)
        {
            return s.Length >= 2 && s[0] == '/' && s[1] == '/';
        }


        private static bool EqualsIgnoreCase(string a, string b)
        {
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                char ca = a[i], cb = b[i];
                if (ca >= 'A' && ca <= 'Z') ca = (char)(ca - 'A' + 'a');
                if (cb >= 'A' && cb <= 'Z') cb = (char)(cb - 'A' + 'a');
                if (ca != cb) return false;
            }

            return true;
        }
    }
}
