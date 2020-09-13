using System;
using System.IO;
using System.Security.Principal;
using System.Text;
using WebSocketSharp.Net;

namespace WebSocketSharp.Server
{
    public class HttpRequestEventArgs : EventArgs
    {
        private HttpListenerContext _context;

        private string _docRootPath;

        public HttpListenerRequest Request => _context.Request;

        public HttpListenerResponse Response => _context.Response;

        public IPrincipal User => _context.User;

        internal HttpRequestEventArgs(HttpListenerContext context, string documentRootPath)
        {
            _context = context;
            _docRootPath = documentRootPath;
        }

        private string createFilePath(string childPath)
        {
            childPath = childPath.TrimStart('/', '\\');
            return new StringBuilder(_docRootPath, 32).AppendFormat("/{0}", childPath).ToString().Replace('\\', '/');
        }

        private static bool tryReadFile(string path, out byte[] contents)
        {
            contents = null;
            if (!File.Exists(path))
            {
                return false;
            }
            try
            {
                contents = File.ReadAllBytes(path);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public byte[] ReadFile(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            if (path.Length == 0)
            {
                throw new ArgumentException("An empty string.", "path");
            }
            if (path.IndexOf("..") > -1)
            {
                throw new ArgumentException("It contains '..'.", "path");
            }
            tryReadFile(createFilePath(path), out byte[] contents);
            return contents;
        }

        public bool TryReadFile(string path, out byte[] contents)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            if (path.Length == 0)
            {
                throw new ArgumentException("An empty string.", "path");
            }
            if (path.IndexOf("..") > -1)
            {
                throw new ArgumentException("It contains '..'.", "path");
            }
            return tryReadFile(createFilePath(path), out contents);
        }
    }
}
