using System.Collections.Specialized;
using System.Text;

namespace WebSocketSharp.Net
{
    internal sealed class QueryStringCollection : NameValueCollection
    {
        public QueryStringCollection()
        {
        }

        public QueryStringCollection(int capacity)
            : base(capacity)
        {
        }

        public static QueryStringCollection Parse(string query)
        {
            return Parse(query, Encoding.UTF8);
        }

        public static QueryStringCollection Parse(string query, Encoding encoding)
        {
            if (query == null)
            {
                return new QueryStringCollection(1);
            }
            if (query.Length == 0)
            {
                return new QueryStringCollection(1);
            }
            if (query == "?")
            {
                return new QueryStringCollection(1);
            }
            if (query[0] == '?')
            {
                query = query.Substring(1);
            }
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            QueryStringCollection queryStringCollection = new QueryStringCollection();
            string[] array = query.Split('&');
            string[] array2 = array;
            foreach (string text in array2)
            {
                int length = text.Length;
                if (length != 0 && !(text == "="))
                {
                    int num = text.IndexOf('=');
                    if (num < 0)
                    {
                        queryStringCollection.Add(null, HttpUtility.UrlDecode(text, encoding));
                        continue;
                    }
                    if (num == 0)
                    {
                        queryStringCollection.Add(null, HttpUtility.UrlDecode(text.Substring(1), encoding));
                        continue;
                    }
                    string name = HttpUtility.UrlDecode(text.Substring(0, num), encoding);
                    int num2 = num + 1;
                    string value = (num2 < length) ? HttpUtility.UrlDecode(text.Substring(num2), encoding) : string.Empty;
                    queryStringCollection.Add(name, value);
                }
            }
            return queryStringCollection;
        }

        public override string ToString()
        {
            if (Count == 0)
            {
                return string.Empty;
            }
            StringBuilder stringBuilder = new StringBuilder();
            string[] allKeys = AllKeys;
            foreach (string text in allKeys)
            {
                stringBuilder.AppendFormat("{0}={1}&", text, base[text]);
            }
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Length--;
            }
            return stringBuilder.ToString();
        }
    }
}
