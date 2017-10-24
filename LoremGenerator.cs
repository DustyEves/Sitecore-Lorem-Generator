using Sitecore.Pipelines.ExpandInitialFieldValue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TechnoBabble.net.LoremGenerator
{
    public class LoremProcessor : ExpandInitialFieldValueProcessor
    {
        private static object syncRoot = new object();
        private static void BuildLoremLines()
        {
            lock(syncRoot) 
            if (_loremLines == null) //recheck loremLines for null in a threadsafe context
            {
                var assembly = Assembly.GetExecutingAssembly();
            
                using (Stream stream = assembly.GetManifestResourceStream("TechnoBabble.net.LoremGenerator.Lorem.txt"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    _loremLines = reader.ReadToEnd()
                        .Split(new char[]{'.'}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(i => string.Format("{0}.  ", i.Trim()))
                        .ToArray();
                }
            }
        }

        private static string[] _loremLines;
        private static string[] loremLines
        {
            get
            {
                if (_loremLines == null)
                    BuildLoremLines();
                return _loremLines;
            }
        }

        private static Random _randomizer = new Random(DateTime.Now.Millisecond * DateTime.Now.Millisecond);
        public string RandomLines(int sentenceCount, int maxLength = 0, bool elipsis = true)
        {
            if (sentenceCount > loremLines.Length)
                return contactStrings(loremLines);

            int maxSkip = loremLines.Length - sentenceCount;

            string rVal = contactStrings(loremLines.Skip(_randomizer.Next(maxSkip)).Take(sentenceCount));
            if (maxLength == 0 || rVal.Length < maxLength)
                return rVal;
            else
                return rVal.Substring(0, maxLength) + (elipsis ? "..." : "");
        }

        private static string contactStrings(IEnumerable<string> input)
        {
            StringBuilder bldr = new StringBuilder();
            foreach (string _line in input)
                bldr.Append(_line);
            return bldr.ToString();
        }

        private Regex rxToken = new Regex("\\$lorem\\-(?<type>length|sentences)\\-(?<size>\\d{1,3})");
        

        public bool testToken(string token)
        {
            return rxToken.IsMatch(token);
        }
        public override void Process(ExpandInitialFieldValueArgs args)
        {
            if (!rxToken.IsMatch(args.SourceField.Value))
                return;

            foreach(Match _match in (rxToken.Matches(args.SourceField.Value)))
            {
                int size = int.Parse(_match.Groups["size"].Value);

                if (_match.Groups["type"].Value == "sentences")
                    args.Result = rxToken.Replace(args.Result, RandomLines(size));

                if (_match.Groups["type"].Value == "length")
                    args.Result = rxToken.Replace(args.Result, RandomLines(size / 10, size, true));
            }

        }

       
    }
}
