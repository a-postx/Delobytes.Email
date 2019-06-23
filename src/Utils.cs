using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Delobytes.Email
{
    public static class Utils
    {
        public static string ListToString(this IList list)
        {
            StringBuilder result = new StringBuilder(string.Empty);

            if (list.Count > 0)
            {
                result.Append(list[0]);
                for (int i = 1; i < list.Count; i++)
                    result.AppendFormat(",{0}", list[i]);
            }
            return result.ToString();
        }

        public static IEnumerable<List<T>> SplitList<T>(this List<T> list, int batchSize)
        {
            for (int i = 0; i < list.Count; i += batchSize)
            {
                yield return list.GetRange(i, Math.Min(batchSize, list.Count - i));
            }
        }

        public static List<string> ExtractEmails(this string inputString)
        {
            string noCommas = Regex.Replace(inputString, @"[\,]", ";", RegexOptions.None);
            string realEmails = Regex.Replace(noCommas, @"[^\w\.\;@-]", string.Empty, RegexOptions.None);
            string[] emails = realEmails.Split(new char[] { ';' }, StringSplitOptions.None);
            List<string> result = new List<string>();

            foreach (string email in emails)
            {
                result.Add(email);
            }

            return result;
        }

        public static bool IsValidEmail(this string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
