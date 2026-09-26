using System;
using System.Threading;

namespace HTMLCrawler
{

    public class HtmlSearcher
    {
        private readonly HtmlNode _root;

        public HtmlSearcher(HtmlNode root)
        {
            _root = root;
        }

        public void FindAndPrint(string path)
        {
            StepInfo[] steps;
            int startIndex;

            if (!PrepareSteps(path, out steps, out startIndex))
            {
                Console.WriteLine("Invalid path.");
                return;
            }

            HtmlNode[] outBuf = new HtmlNode[32];
            int outCount = 0;

            MatchAnywhere(_root, steps, startIndex, ref outBuf, ref outCount);
            PrintMatchesOrFooter(outBuf, outCount);
        }

        public void CollectMatches(string path, out HtmlNode[] matches, out int count)
        {
            matches = new HtmlNode[32];
            count = 0;

            StepInfo[] steps;
            int startIndex;

            if (!PrepareSteps(path, out steps, out startIndex))
            {
                return;
            }

            MatchAnywhere(_root, steps, startIndex, ref matches, ref count);
        }


        public void FindAndPrintParallel(string path, int threadsCount)
        {
            StepInfo[] steps;
            int startIndex;

            if (!PrepareSteps(path, out steps, out startIndex))
            {
                ConsolePrinter.Line("Invalid path.");
                return;
            }

            if (threadsCount < 1) threadsCount = 1;


            var queue = new WorkQueue<HtmlNode>();
            var results = new ResultList<HtmlNode>();

            HtmlNode? child = _root.Child;
            while (child != null)
            {
                queue.Enqueue(child);
                child = child.Next;
            }


            ConsolePrinter.Line("");
            ConsolePrinter.Line("--- Parallel search started ---");
            ConsolePrinter.Line("Threads: " + threadsCount);
            ConsolePrinter.Line("Path: " + path);
            ConsolePrinter.Line("-------------------------------------------------");


            int activeWorkers = threadsCount;
            int t0 = Environment.TickCount;
            const bool DEBUG_THREADS = true;

            Thread[] workers = new Thread[threadsCount];

            for (int t = 0; t < threadsCount; t++)
            {
                workers[t] = new Thread(() =>
                {
                    HtmlNode node;
                    HtmlNode[] local = new HtmlNode[16];
                    int localCount = 0;


                    while (queue.TryDequeue(out node))
                    {
                        MatchAnywhere(node, steps, startIndex, ref local, ref localCount);

                        for (int i = 0; i < localCount; i++)
                        {
                            HtmlNode n = local[i];

                            if (DEBUG_THREADS)
                            {
                                string preview;
                                if (n.TagName == "#text")
                                {
                                    preview = "\"" + (n.InnerText ?? "") + "\"";
                                }
                                else if (n.Child == null && n.InnerText != null)
                                {
                                    preview = "\"" + n.InnerText + "\"";
                                }
                                else
                                {
                                    preview = "<inner>";
                                }

                                ConsolePrinter.Line(
                                    ThreadLabel.Get() + "  <" + n.TagName + "> -> " + preview
                                );
                            }

                            results.Add(n);
                        }

                        localCount = 0;
                    }

                    Interlocked.Decrement(ref activeWorkers);
                });

                workers[t].IsBackground = true;
                workers[t].Start();
            }


            while (activeWorkers > 0)
            {
                Thread.Sleep(10);
            }
            int t1 = Environment.TickCount;

  
            HtmlNode[] rootMatchBuf = new HtmlNode[32];
            int rootMatchCount = 0;

            MatchFromHere(_root, steps, startIndex, ref rootMatchBuf, ref rootMatchCount);

            for (int i = 0; i < rootMatchCount; i++)
            {
                results.Add(rootMatchBuf[i]);
            }

       
            ConsolePrinter.Line("-------------------------------------------------");
            ConsolePrinter.Line("Total matches: " + results.Count);

            if (results.Count > 0)
            {
                ConsolePrinter.Line("Matches (normalized output):");
                results.ForEach(delegate (HtmlNode n)
                {
                    if (n.TagName == "#text")
                    {
                        if (n.InnerText != null)
                            ConsolePrinter.Line(n.InnerText);
                    }
                    else if (n.Child == null)
                    {
                        if (n.InnerText != null)
                            ConsolePrinter.Line(n.InnerText);
                    }
                    else
                    {
                       
                        ConsolePrinter.Line(n.RenderInner());
                    }
                });
            }

            int elapsed = t1 - t0;
            if (elapsed < 0) elapsed = 0;

            ConsolePrinter.Line("Elapsed: " + elapsed + " ms");
            ConsolePrinter.Line("--- End of parallel search ---");
            ConsolePrinter.Line("");
        }


        private bool PrepareSteps(string path, out StepInfo[] steps, out int startIndex)
        {
            steps = new StepInfo[0];
            startIndex = 0;

            if (path == null || path.Length < 2)
            {
                return false;
            }

            string[] rawSteps = SplitPath(path);

            int start = 0;
            while (start < rawSteps.Length && rawSteps[start].Length == 0) start++;

            if (start >= rawSteps.Length)
            {
                return false;
            }

            steps = ParseAllSteps(rawSteps);
            startIndex = start;
            return true;
        }


        private void MatchAnywhere(HtmlNode node, StepInfo[] steps, int idx,
                                   ref HtmlNode[] outBuf, ref int outCount)
        {
            if (node == null) return;

       
            MatchFromHere(node, steps, idx, ref outBuf, ref outCount);

         
            HtmlNode? ch = node.Child;
            while (ch != null)
            {
                MatchAnywhere(ch, steps, idx, ref outBuf, ref outCount);
                ch = ch.Next;
            }
        }


        private void MatchFromHere(HtmlNode node, StepInfo[] steps, int idx,
                                   ref HtmlNode[] outBuf, ref int outCount)
        {
            if (idx >= steps.Length)
            {
                Ensure(ref outBuf, outCount + 1);
                outBuf[outCount++] = node;
                return;
            }

            StepInfo si = steps[idx];

            bool nameOk = (si.NameLen == 1 && si.NameFirst == '*') ||
                          NameEquals(si, node.TagName);
            if (!nameOk) return;

            if (si.HasAttr && !node.Attributes.ValueEquals(si.AttrName, si.AttrValue))
                return;

            if (idx == steps.Length - 1)
            {
              
                Ensure(ref outBuf, outCount + 1);
                outBuf[outCount++] = node;
                return;
            }

            StepInfo next = steps[idx + 1];

            HtmlNode? child = node.Child;
            int order = 0;

            while (child != null)
            {
                bool candidate = (next.NameLen == 1 && next.NameFirst == '*') ||
                                 NameEquals(next, child.TagName);

                if (candidate)
                {
                    bool passAttr = true;
                    if (next.HasAttr)
                    {
                        passAttr = child.Attributes.ValueEquals(next.AttrName, next.AttrValue);
                    }

                    if (passAttr)
                    {
                        order++;
                        if (next.Index <= 0 || order == next.Index)
                        {
                            MatchFromHere(child, steps, idx + 1, ref outBuf, ref outCount);
                        }
                    }
                }

                child = child.Next;
            }
        }



        private struct StepInfo
        {
            public string Name;   
            public int NameLen;    
            public char NameFirst; 
            public bool HasAttr;
            public string AttrName;
            public string AttrValue;
            public int Index;     
        }

        private StepInfo[] ParseAllSteps(string[] raw)
        {
            StepInfo[] a = new StepInfo[raw.Length];

            for (int i = 0; i < raw.Length; i++)
            {
                ParseStep(raw[i], out a[i]);
                a[i].NameLen = a[i].Name.Length;
                a[i].NameFirst = (a[i].NameLen > 0) ? a[i].Name[0] : '\0';
            }

            return a;
        }

        private void ParseStep(string raw, out StepInfo si)
        {
            si = new StepInfo
            {
                Name = raw,
                HasAttr = false,
                AttrName = "",
                AttrValue = "",
                Index = -1
            };

            int br = IndexOfChar(raw, '[');
            if (br != -1)
            {
                si.Name = SubstringSafe(raw, 0, br);
            }

            int lb = br;
            int rb = LastIndexOfChar(raw, ']');

            if (lb != -1 && rb != -1 && rb > lb + 1)
            {
                string inside = TrimManual(SubstringSafe(raw, lb + 1, rb - lb - 1));

                if (inside.Length > 0)
                {
                    if (inside[0] == '@')
                    {
                    
                        int eq = IndexOfChar(inside, '=');
                        if (eq != -1)
                        {
                            string attr = TrimManual(SubstringSafe(inside, 1, eq - 1));
                            string val = TrimManual(
                                SubstringSafe(inside, eq + 1, inside.Length - (eq + 1))
                            );
                            val = StripOuterQuotes(val);

                            si.HasAttr = true;
                            si.AttrName = attr;
                            si.AttrValue = val;
                        }
                    }
                    else
                    {
                     
                        int num = 0;
                        for (int i = 0; i < inside.Length; i++)
                        {
                            char c = inside[i];
                            if (c >= '0' && c <= '9')
                            {
                                num = num * 10 + (c - '0');
                            }
                        }
                        si.Index = (num > 0) ? num : -1;
                    }
                }
            }
        }


        private bool NameEquals(StepInfo s, string tag)
        {
            if (s.NameLen != tag.Length) return false;

            for (int i = 0; i < s.NameLen; i++)
            {
                char a = s.Name[i];
                char b = tag[i];

                if (a >= 'A' && a <= 'Z') a = (char)(a - 'A' + 'a');
                if (b >= 'A' && b <= 'Z') b = (char)(b - 'A' + 'a');

                if (a != b) return false;
            }

            return true;
        }

    

        private void PrintMatchesOrFooter(HtmlNode[] outBuf, int outCount)
        {
            if (outCount == 0)
            {
                Console.WriteLine("--- End of search ---");
                return;
            }

            for (int i = 0; i < outCount; i++)
            {
                HtmlNode node = outBuf[i];

                if (node.TagName == "#text")
                {
                    if (node.InnerText != null)
                        Console.WriteLine(node.InnerText);
                }
                else if (node.Child == null)
                {
                    if (node.InnerText != null)
                        Console.WriteLine(node.InnerText);
                }
                else
                {
                 
                    Console.WriteLine(node.RenderInner());
                }
            }

            Console.WriteLine();
            Console.WriteLine("--- End of search ---");
        }

        private string[] SplitPath(string path)
        {
            string[] tmp = new string[8];
            int cnt = 0;
            var cur = new CharBuf();

            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
                if (c == '/')
                {
                    if (cnt == tmp.Length)
                    {
                        tmp = Grow(tmp, cnt);
                    }
                    tmp[cnt++] = cur.ToString();
                    cur.Clear();
                }
                else
                {
                    cur.Add(c);
                }
            }

            if (cnt == tmp.Length)
            {
                tmp = Grow(tmp, cnt);
            }
            tmp[cnt++] = cur.ToString();

            string[] res = new string[cnt];
            for (int i = 0; i < cnt; i++)
            {
                res[i] = tmp[i];
            }
            return res;
        }

        private static string[] Grow(string[] a, int n)
        {
            string[] b = new string[a.Length * 2];
            for (int i = 0; i < n; i++)
            {
                b[i] = a[i];
            }
            return b;
        }

        private static void Ensure(ref HtmlNode[] a, int need)
        {
            if (need <= a.Length) return;

            HtmlNode[] b = new HtmlNode[a.Length * 2];
            for (int i = 0; i < a.Length; i++)
            {
                b[i] = a[i];
            }
            a = b;
        }

        private static int IndexOfChar(string s, char ch)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ch) return i;
            }
            return -1;
        }

        private static int LastIndexOfChar(string s, char ch)
        {
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] == ch) return i;
            }
            return -1;
        }

        private static string SubstringSafe(string s, int start, int length)
        {
            if (start < 0) start = 0;
            if (length < 0) length = 0;
            if (start > s.Length) start = s.Length;
            if (start + length > s.Length) length = s.Length - start;

            var r = new CharBuf();
            for (int i = 0; i < length; i++)
            {
                r.Add(s[start + i]);
            }
            return r.ToString();
        }

        private static string StripOuterQuotes(string s)
        {
            if (s.Length >= 2)
            {
                char f = s[0];
                char l = s[s.Length - 1];

                if ((f == '"' && l == '"') || (f == '\'' && l == '\''))
                {
                    return SubstringSafe(s, 1, s.Length - 2);
                }
            }
            return s;
        }

        private static string TrimManual(string s)
        {
            int a = 0;
            int b = s.Length - 1;

            while (a <= b && IsWs(s[a])) a++;
            while (b >= a && IsWs(s[b])) b--;

            if (b < a) return "";

            var r = new CharBuf();
            for (int i = a; i <= b; i++)
            {
                r.Add(s[i]);
            }
            return r.ToString();
        }

        private static bool IsWs(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }
    }
}
