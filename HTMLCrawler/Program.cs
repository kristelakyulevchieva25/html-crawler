using System;
using System.IO;

namespace HTMLCrawler
{
    internal class Program
    {
        static void Main(string[] args)
        {
          
            if (args == null || args.Length < 1 || IsNullOrWhiteSpaceManual(args[0]))
            {
                ConsolePrinter.Line("Usage: HTMLCrawler <path-to-html-file>");
                ConsolePrinter.Line("Example: HTMLCrawler \"C:\\temp\\page.html\"");
                return;
            }

            string path = StripOuterQuotes(TrimManual(args[0]));


            if (!File.Exists(path))
            {
                ConsolePrinter.Line("File not found: " + path);
                return;
            }


            string baseFolder = ExtractFolder(path);

            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var parser = new HtmlParser(fs);
                    HtmlNode docRoot = parser.Parse();

                    if (docRoot == null)
                    {
                        ConsolePrinter.Line("Parse error: empty or invalid HTML document.");
                        return;
                    }

     
                    bool valid = HtmlNode.ValidateHtml(docRoot);
                    if (!valid)
                    {
                        ConsolePrinter.Line("Warning: HTML validation reported problems.");
                    }

              
                    var exec = new CommandExecutor(docRoot, baseFolder);
                    ConsolePrinter.Line("HTML Crawler ready.");
                    ConsolePrinter.Line("Commands:");
                    ConsolePrinter.Line("  PRINT  \"//path\"");
                    ConsolePrinter.Line("  PRINTP \"//path\" [threads]");
                    ConsolePrinter.Line("  SET    \"//path\" \"text | <html-fragment>\"");
                    ConsolePrinter.Line("  COPY   \"//source-path\" \"//target-path\"");
                    ConsolePrinter.Line("  SAVE   \"archive.huff\"");
                    ConsolePrinter.Line("  LOAD   \"archive.huff\"");
                    ConsolePrinter.Line("  EXIT");

                    while (true)
                    {
                        ConsolePrinter.Raw("> ");
                        string line = Console.ReadLine();
                        if (line == null) break;
                        if (IsNullOrWhiteSpaceManual(line)) continue;

                        try
                        {
                            exec.Execute(line);
                        }
                        catch (Exception ex)
                        {
                            ConsolePrinter.Line("Command error: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConsolePrinter.Line("I/O error: " + ex.Message);
            }
        }



        private static bool IsNullOrWhiteSpaceManual(string s)
        {
            if (s == null) return true;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
                    return false;
            }
            return true;
        }

        private static string TrimManual(string s)
        {
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

        private static bool IsWs(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        private static string StripOuterQuotes(string s)
        {
            if (s.Length >= 2)
            {
                char f = s[0];
                char l = s[s.Length - 1];

                if ((f == '"' && l == '"') || (f == '\'' && l == '\''))
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

        private static string ExtractFolder(string path)
        {
            if (path == null) return "";

            int lastSep = -1;
            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
                if (c == '\\' || c == '/')
                {
                    lastSep = i;
                }
            }

            if (lastSep <= 0) return "";

            var buf = new CharBuf();
            for (int i = 0; i < lastSep; i++)
            {
                buf.Add(path[i]);
            }
            return buf.ToString();
        }
    }
}
