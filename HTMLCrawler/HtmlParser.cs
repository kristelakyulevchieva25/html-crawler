using System;
using System.IO;

namespace HTMLCrawler
{

    public class HtmlParser
    {
        private readonly Stream _stream;

   
        private bool _hasPeek;
        private int _peekByte;

      
        private bool _hadError;

        public HtmlParser(Stream stream)
        {
            _stream = stream;
            _hadError = false;
            _hasPeek = false;
            _peekByte = -1;
        }

        public HtmlNode Parse(out bool ok)
        {
            _hadError = false;

            var root = new HtmlNode("root");
            var stack = new MyStack<HtmlNode>();
            stack.Push(root);

            var textBuf = new CharBuf();

            while (true)
            {
                int b = Read();
                if (b == -1)
                {
                    FlushTextIfAny(stack, textBuf);
                    break;
                }

                if (b != '<')
                {
                    textBuf.Add((char)b);
                    continue;
                }

                FlushTextIfAny(stack, textBuf);

                int next = Peek();
                if (next == -1)
                {
                    ReportError("Error: unexpected EOF after '<'.");
                    break;
                }

          
                if (next == '!')
                {
                    Read(); 

                 
                    if (Peek() == '-')
                    {
                        Read(); 
                        if (Peek() == '-')
                        {
                            Read(); 
                            SkipHtmlComment();
                            continue;
                        }
                    }

              
                    SkipUntil('>');
                    continue;
                }

          
                if (next == '?')
                {
                    Read();
                    SkipUntilQMarkClose();
                    continue;
                }

            
                if (next == '/')
                {
                    Read();

                    string closeName = ReadName();
                    SkipSpaces();
                    Expect('>');

                    if (stack.Count <= 1)
                    {
                        ReportError("Error: closing tag </" + closeName + "> without matching opening tag.");
                        continue;
                    }

                    HtmlNode top = stack.Pop();
                    if (!Utils.EqualsIgnoreCase(top.TagName, closeName))
                    {
                        var msg = new CharBuf();
                        msg.Add("Error: tag mismatch. Expected </");
                        msg.Add(top.TagName);
                        msg.Add(">, got </");
                        msg.Add(closeName);
                        msg.Add(">.");
                        ReportError(msg.ToString());
                    }
                }
                else
                {
         
                    string tag = ReadName();
                    if (tag.Length == 0)
                    {
                        ReportError("Error: empty tag name after '<'.");
                        SkipUntil('>');
                        continue;
                    }

                    var node = new HtmlNode(Utils.NormalizeTag(tag));

                 
                    while (true)
                    {
                        SkipSpaces();
                        int c = Peek();

                        if (c == -1)
                        {
                            ReportError("Error: unexpected EOF inside tag <" + tag + ">.");
                            break;
                        }

                        if (c == '>' || c == '/') break;

                        string attrName = ReadAttrName();
                        if (attrName.Length == 0)
                        {
                            ReportError("Error: invalid attribute name in <" + tag + ">.");
                            SkipUntil('>');
                            break;
                        }

                        SkipSpaces();

                        char quote = '"';
                        string attrValue = "";

                        if (Peek() == '=')
                        {
                            Read(); 
                            SkipSpaces();

                            int qb = Read();
                            if (qb == -1)
                            {
                                ReportError("Error: unexpected EOF after '=' in attribute of <" + tag + ">.");
                                break;
                            }

                            quote = (char)qb;

                            if (quote != '"' && quote != '\'')
                            {
                            
                                ReportError("Error: expected quote after '=' in <" + tag + ">.");

                                var tmp = new CharBuf();
                                tmp.Add((char)qb);            
                                tmp.Add(ReadUnquotedUntilStop()); 

                                quote = '"'; 
                                attrValue = tmp.ToString();
                            }
                            else
                            {
                                attrValue = ReadQuoted(quote);
                            }
                        }

                        node.AddAttribute(attrName, attrValue, quote);
                    }

                    bool selfClose = false;
                    if (Peek() == '/')
                    {
                        Read(); 
                        selfClose = true;
                    }

                    Expect('>');

                    if (IsVoidTagName(tag))
                    {
                        selfClose = true;
                    }

                    HtmlNode parent = stack.Peek();
                    parent.AddChild(node);

                    if (!selfClose)
                    {
                        stack.Push(node);
                    }
                }
            }

            if (stack.Count != 1)
            {
                ReportError("Error: unclosed tags at EOF.");
            }

            ok = !_hadError;
            return root.Child ?? root;
        }

        public HtmlNode Parse()
        {
            bool ok;
            HtmlNode r = Parse(out ok);
            return r;
        }

        private void FlushTextIfAny(MyStack<HtmlNode> stack, CharBuf buf)
        {
            if (buf.Length == 0) return;

            string text = TrimSoft(buf.ToString());
            buf.Clear();

            if (text.Length == 0) return;

            HtmlNode parent = stack.Peek();
            if (parent.Child == null &&
                (parent.InnerText == null || parent.InnerText.Length == 0))
            {
                parent.InnerText = text;
            }
            else
            {
                parent.AddChild(new HtmlNode("#text", text));
            }
        }

        private int Read()
        {
            if (_hasPeek)
            {
                _hasPeek = false;
                return _peekByte;
            }
            return _stream.ReadByte();
        }

        private int Peek()
        {
            if (_hasPeek) return _peekByte;

            _peekByte = _stream.ReadByte();
            _hasPeek = true;
            return _peekByte;
        }

        private void Expect(char ch)
        {
            int b = Read();
            if (b != ch)
            {
                if (b == -1)
                {
                    ReportError("Error: expected '" + ch + "' but got EOF.");
                    return;
                }

                var msg = new CharBuf();
                msg.Add("Error: expected '");
                msg.Add(ch);
                msg.Add("' but got '");
                msg.Add((char)b);
                msg.Add("'.");
                ReportError(msg.ToString());
            }
        }

        private void SkipSpaces()
        {
            int b = Peek();
            while (b == ' ' || b == '\t' || b == '\r' || b == '\n')
            {
                Read();
                b = Peek();
            }
        }

        private string ReadName()
        {
            var name = new CharBuf();
            int b = Peek();

            while (IsNameChar(b))
            {
                name.Add((char)Read());
                b = Peek();
            }

            return name.ToString();
        }

        private string ReadAttrName()
        {
            var name = new CharBuf();
            int b = Peek();

            while (IsAttrNameChar(b))
            {
                name.Add((char)Read());
                b = Peek();
            }

            return name.ToString();
        }

        private bool IsNameChar(int b)
        {
            if (b == -1) return false;
            char c = (char)b;
            if (c >= 'a' && c <= 'z') return true;
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= '0' && c <= '9') return true;
            if (c == '-' || c == '_' || c == ':') return true;
            return false;
        }

        private bool IsAttrNameChar(int b)
        {
            if (b == -1) return false;
            char c = (char)b;

            if (c == '=') return false;
            if (c == '>') return false;
            if (c == '/') return false;
            if (c == '"') return false;
            if (c == '\'') return false;
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n') return false;

            return true;
        }

        private string ReadQuoted(char quote)
        {
            var result = new CharBuf();

            while (true)
            {
                int b = Read();
                if (b == -1)
                {
                    ReportError("Error: EOF reached inside quoted attribute value.");
                    break;
                }

                char c = (char)b;
                if (c == quote) break;

                result.Add(c);
            }

            return result.ToString();
        }

        private string ReadUnquotedUntilStop()
        {
            var result = new CharBuf();
            int b = Peek();

            while (b != -1)
            {
                char c = (char)b;

                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') break;
                if (c == '>' || c == '/') break;

                result.Add((char)Read());
                b = Peek();
            }

            return result.ToString();
        }

        private string TrimSoft(string s)
        {
            int start = 0;
            int end = s.Length - 1;

            while (start <= end &&
                   (s[start] == ' ' || s[start] == '\t' || s[start] == '\r' || s[start] == '\n'))
            {
                start++;
            }

            while (end >= start &&
                   (s[end] == ' ' || s[end] == '\t' || s[end] == '\r' || s[end] == '\n'))
            {
                end--;
            }

            if (end < start) return "";

            var buf = new CharBuf();
            for (int i = start; i <= end; i++)
            {
                buf.Add(s[i]);
            }

            return buf.ToString();
        }

        private bool IsVoidTagName(string tag)
        {
            string t = Utils.ToLowerAscii(tag);

            return t == "img" ||
                   t == "br" ||
                   t == "hr" ||
                   t == "meta" ||
                   t == "link" ||
                   t == "input" ||
                   t == "source" ||
                   t == "wbr" ||
                   t == "base" ||
                   t == "area" ||
                   t == "col" ||
                   t == "embed" ||
                   t == "param" ||
                   t == "track";
        }

        private void SkipHtmlComment()
        {
            int dashState = 0;

            while (true)
            {
                int b = Read();
                if (b == -1)
                {
                    ReportError("Error: EOF inside HTML comment.");
                    break;
                }

                char c = (char)b;

                if (c == '-' && dashState == 0) dashState = 1;
                else if (c == '-' && dashState == 1) dashState = 2;
                else if (c == '>' && dashState == 2) break;
                else dashState = 0;
            }
        }

        private void SkipUntil(char end)
        {
            int b = Read();
            while (b != -1 && b != end)
            {
                b = Read();
            }

            if (b == -1)
            {
                ReportError("Error: EOF reached while skipping until '" + end + "'.");
            }
        }

        private void SkipUntilQMarkClose()
        {
            int prev = -1;
            int cur = Read();

            while (cur != -1)
            {
                if (prev == '?' && cur == '>')
                {
                    return;
                }

                prev = cur;
                cur = Read();
            }

            ReportError("Error: EOF inside processing instruction (expected '?>').");
        }

        private void ReportError(string message)
        {
            _hadError = true;
            ConsolePrinter.Line(message);
        }
    }
}
