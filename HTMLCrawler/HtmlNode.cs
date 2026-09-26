using System;

namespace HTMLCrawler
{

    public class HtmlNode
    {
     
        private class NodeData
        {
            public string TagName;
            public string? InnerText;
            public AttributeTable Attributes;
            public HtmlNode? Child;
            public HtmlNode? Next;
            public HtmlNode? LastChild;

       
            public bool Shared;

            public NodeData(string tagName, string? innerText)
            {
                TagName = (tagName == "#text") ? "#text" : Utils.NormalizeTag(tagName);
                InnerText = innerText;
                Attributes = new AttributeTable();
                Child = null;
                Next = null;
                LastChild = null;
                Shared = false; 
            }

    
            public NodeData(NodeData original)
            {
                TagName = original.TagName;
                InnerText = original.InnerText;
                Attributes = original.Attributes.Clone();
                Child = original.Child;
                Next = original.Next;
                LastChild = original.LastChild;
                Shared = false; 
            }
        }

        private NodeData _data;

        public HtmlNode(string tagName, string? innerText = null)
        {
            _data = new NodeData(tagName, innerText);
        }


        private HtmlNode(NodeData shared)
        {
            _data = shared;
        }


        public string TagName { get { return _data.TagName; } }
        public AttributeTable Attributes { get { return _data.Attributes; } }
        public HtmlNode? Child { get { return _data.Child; } }
        public HtmlNode? Next { get { return _data.Next; } }

        public string? InnerText
        {
            get { return _data.InnerText; }
            set
            {
                EnsureDataIsUnshared();
                _data.InnerText = value;
                _data.Child = null;
                _data.LastChild = null;
            }
        }


        private void EnsureDataIsUnshared()
        {
            if (!_data.Shared) return;

          
            _data = new NodeData(_data);
        }


        private static void SetNext(HtmlNode node, HtmlNode? newNext)
        {
            node.EnsureDataIsUnshared();
            node._data.Next = newNext;
        }


        private static void MarkSharedDeep(HtmlNode? n)
        {
            while (n != null)
            {
                n._data.Shared = true;

                if (n._data.Child != null)
                {
                    MarkSharedDeep(n._data.Child);
                }

                n = n._data.Next;
            }
        }


        public HtmlNode ShallowClone()
        {
 
            _data.Shared = true;

            MarkSharedDeep(_data.Child);

            return new HtmlNode(_data);
        }


        public HtmlNode DeepClone()
        {
            var newNode = new HtmlNode(TagName, InnerText);
            newNode._data.Attributes = _data.Attributes.Clone();

            newNode._data.Child = null;
            newNode._data.LastChild = null;
            newNode._data.Next = null;

            HtmlNode? sourceChild = _data.Child;
            while (sourceChild != null)
            {
                HtmlNode clonedChild = sourceChild.DeepClone();
                newNode.AddChild(clonedChild);
                sourceChild = sourceChild.Next;
            }

            return newNode;
        }

        public void AddAttribute(string key, string value, char quote)
        {
            EnsureDataIsUnshared();
            _data.Attributes.Add(key, value, quote);
        }


        public void AddChild(HtmlNode childNode)
        {
            EnsureDataIsUnshared();

            if (_data.Child == null)
            {
                _data.Child = _data.LastChild = childNode;
                return;
            }

            SetNext(_data.LastChild!, childNode);
            _data.LastChild = childNode;
        }

       
        public void ReplaceInnerWithText(string newText)
        {
            EnsureDataIsUnshared();
            _data.InnerText = newText ?? "";
            _data.Child = null;
            _data.LastChild = null;
        }

        public void ReplaceInnerWithChildren(HtmlNode? firstChild)
        {
            EnsureDataIsUnshared();

            _data.InnerText = null;
            _data.Child = null;
            _data.LastChild = null;

            HtmlNode? cur = firstChild;
            while (cur != null)
            {
                HtmlNode? next = cur.Next;
                SetNext(cur, null);
                AddChild(cur);
                cur = next;
            }
        }


        public void PrintSubtree(int level = 0)
        {
            if (_data.TagName == "#text")
            {
                ConsolePrinter.Line(MakeIndent(level) + (_data.InnerText ?? ""));
                return;
            }

            string indent = MakeIndent(level);
            string attrs = _data.Attributes.BuildAttributeString();

            if (IsVoidTag())
            {
                ConsolePrinter.Line(indent + "<" + _data.TagName + attrs + "/>");
                return;
            }

            if (!IsNullOrEmpty(_data.InnerText) && _data.Child == null)
            {
                ConsolePrinter.Line(
                    indent + "<" + _data.TagName + attrs + ">" +
                    _data.InnerText +
                    "</" + _data.TagName + ">"
                );
                return;
            }

            bool canCollapse =
                _data.Child != null &&
                _data.Child.Next == null &&
                _data.Child.Child == null &&
                !IsNullOrEmpty(_data.Child.InnerText) &&
                !Utils.EqualsIgnoreCase(_data.TagName, "tr");

            if (canCollapse)
            {
                string cAttrs = _data.Child.Attributes.BuildAttributeString();
                ConsolePrinter.Line(
                    indent + "<" + _data.TagName + attrs + ">" +
                    "<" + _data.Child.TagName + cAttrs + ">" +
                    _data.Child.InnerText +
                    "</" + _data.Child.TagName + ">" +
                    "</" + _data.TagName + ">"
                );
                return;
            }

            ConsolePrinter.Line(indent + "<" + _data.TagName + attrs + ">");
            if (_data.Child != null)
            {
                _data.Child.Print(level + 1);
            }
            ConsolePrinter.Line(indent + "</" + _data.TagName + ">");
        }

        public void Print(int level = 0)
        {
            PrintSubtree(level);

            if (_data.Next != null)
            {
                _data.Next.Print(level);
            }
        }

        public string RenderInline()
        {
            if (_data.TagName == "#text")
            {
                return _data.InnerText ?? "";
            }

            string attrs = _data.Attributes.BuildAttributeString();

            if (IsVoidTag())
            {
                return "<" + _data.TagName + attrs + "/>";
            }

            var buf = new CharBuf();
            buf.Add("<");
            buf.Add(_data.TagName);
            buf.Add(attrs);
            buf.Add(">");

            if (!IsNullOrEmpty(_data.InnerText) && _data.Child == null)
            {
                buf.Add(_data.InnerText!);
            }
            else
            {
                HtmlNode? cur = _data.Child;
                while (cur != null)
                {
                    buf.Add(cur.RenderInline());
                    cur = cur.Next;
                }
            }

            buf.Add("</");
            buf.Add(_data.TagName);
            buf.Add(">");
            return buf.ToString();
        }

        public string RenderInner()
        {
            var buf = new CharBuf();
            HtmlNode? ch = _data.Child;

            while (ch != null)
            {
                if (ch.TagName == "#text")
                {
                    if (!IsNullOrEmpty(ch.InnerText))
                    {
                        buf.Add(ch.InnerText!);
                    }
                }
                else
                {
                    string attrs = ch.Attributes.BuildAttributeString();

                    if (ch.IsVoidTag())
                    {
                        buf.Add("<"); buf.Add(ch.TagName); buf.Add(attrs); buf.Add("/>");
                    }
                    else if (!IsNullOrEmpty(ch.InnerText) && ch.Child == null)
                    {
                        buf.Add("<"); buf.Add(ch.TagName); buf.Add(attrs); buf.Add(">");
                        buf.Add(ch.InnerText!);
                        buf.Add("</"); buf.Add(ch.TagName); buf.Add(">");
                    }
                    else
                    {
                        buf.Add("<"); buf.Add(ch.TagName); buf.Add(attrs); buf.Add(">");
                        buf.Add(ch.RenderInner());
                        buf.Add("</"); buf.Add(ch.TagName); buf.Add(">");
                    }
                }

                ch = ch.Next;
            }

            return buf.ToString();
        }


        public static bool ValidateHtml(HtmlNode root)
        {
            if (root == null) return false;
            return ValidateNode(root);
        }

        private static bool ValidateNode(HtmlNode node)
        {
            if (node.InnerText != null)
            {
                for (int i = 0; i < node.InnerText.Length; i++)
                {
                    if (node.InnerText[i] == '<')
                    {
                        ConsolePrinter.Line("Warning: suspicious '<' inside <" + node.TagName + "> text.");
                        return false;
                    }
                }
            }

            if (node.Child != null && !ValidateNode(node.Child)) return false;
            if (node.Next != null && !ValidateNode(node.Next)) return false;

            return true;
        }


        private static string MakeIndent(int level)
        {
            int n = level * 2;
            var buf = new CharBuf();
            for (int i = 0; i < n; i++) buf.Add(' ');
            return buf.ToString();
        }

        private static bool IsNullOrEmpty(string? s)
        {
            return s == null || s.Length == 0;
        }

        private bool IsVoidTag()
        {
            string tagName = _data.TagName;

            if (tagName == "img") return true;
            if (tagName == "br") return true;
            if (tagName == "hr") return true;
            if (tagName == "meta") return true;
            if (tagName == "link") return true;
            if (tagName == "input") return true;
            if (tagName == "source") return true;
            if (tagName == "wbr") return true;
            if (tagName == "base") return true;
            if (tagName == "area") return true;
            if (tagName == "col") return true;
            if (tagName == "embed") return true;
            if (tagName == "param") return true;
            if (tagName == "track") return true;

            return false;
        }
    }
}
