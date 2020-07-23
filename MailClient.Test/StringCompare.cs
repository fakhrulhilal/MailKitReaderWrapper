using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MailClient.Test
{
    internal class StringCompare
    {
        internal static IEqualityComparer<string> IgnoreLineBreak => new IgnoreLineBreakComparer();

        public class IgnoreLineBreakComparer : IEqualityComparer<string>
        {
            public bool Equals(string word1, string word2)
            {
                if (word1 == null && word2 == null) return true;
                if (word1 == null || word2 == null) return false;
                if (word1 == word2) return true;
                string removed1 = RemoveLineBreak(word1);
                string removed2 = RemoveLineBreak(word2);
                return removed1 == removed2;
            }

            private string RemoveLineBreak(string word) => Regex.Replace(word, @"(\r\n|\r|\n)", string.Empty);

            public int GetHashCode(string obj) => obj.GetHashCode();
        }
    }
}
