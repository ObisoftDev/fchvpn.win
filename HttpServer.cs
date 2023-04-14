using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObisoftNet.Http
{

    public delegate void HttpServerHandle(HttpListenerRequest request, HttpListenerResponse response, RouteResult result);

    public class HttpServer : IDisposable
    {
        private HttpListener _server;
        private HttpServerHandle _handle;
        public bool _runing = true;

        public string Host { get; private set; } = "";
        public HttpServer(HttpServerHandle handle = null)
        {
            _server = new HttpListener();
            _handle = handle;
        }

        public void Run(int port = 80)
        {
            ServicePointManager.DefaultConnectionLimit = 100;
            Host = $"http://127.0.0.1:{port}/";
            _server.Prefixes.Add(Host);
            if (port == 80)
                Host = $"http://127.0.0.1";
            _server.Start();
            _server.BeginGetContext(HandleContext, null);
        }


        private Dictionary<string, HttpServerHandle> routes = new Dictionary<string, HttpServerHandle>();
        public void Route(string path, HttpServerHandle handleroute)
        {
            if (path == "/")
                _handle = handleroute;
            routes.Add(path, handleroute);
        }

        private List<Thread> threads = new List<Thread>();
        private void HandleContext(IAsyncResult ar)
        {
            try
            {
                var context = _server.EndGetContext(ar);
                if (_handle != null)
                {
                    Thread t = new Thread(() => {
                        try
                        {
                            bool next = false;
                            foreach (var route in routes)
                            {
                                if (context.Request.Url.AbsolutePath == route.Key)
                                {
                                    var routeresult = RouteResult.ResultFrom(context.Request);
                                    route.Value(context.Request, context.Response, routeresult);
                                    next = true;
                                    break;
                                }
                            }
                            if (_handle != null && !next)
                            {
                                var routeresult = RouteResult.ResultFrom(context.Request);
                                _handle(context.Request, context.Response, routeresult);
                            }
                        }
                        catch
                        {

                        }

                        try
                        {
                                context.Response?.Close();
                        }
                        catch (Exception e)
                        {
                        }
                    });
                    threads.Add(t);
                    t.Start();
                }
            }
            catch 
            {
            }

            if (_runing)
            _server.BeginGetContext(HandleContext, null);
        }

        public void Dispose()
        {
            try
            {
                foreach (var t in threads)
                    t?.Abort();
                threads?.Clear();
                _server?.Abort();
                _runing = false;
            }
            catch
            {
                
            }
        }
    }
}