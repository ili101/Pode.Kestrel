using System;
using System.Net;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PodeKestrel
{
    public class PodeListener : IDisposable
    {
        public bool IsListening { get; private set; }
        public bool IsDisposed { get; private set; }
        public bool ErrorLoggingEnabled { get; set; }
        public CancellationToken CancellationToken { get; private set; }
        public PodeListenerType Type { get; private set; }

        private IList<PodeSocket> Sockets;
        private BlockingCollection<PodeContext> Contexts;
        private WebHostBuilder WebBuilder;
        private IWebHost WebHost;

        public PodeListener(CancellationToken cancellationToken, PodeListenerType type = PodeListenerType.Http)
        {
            CancellationToken = cancellationToken;
            IsDisposed = false;
            Type = type;

            WebBuilder = new WebHostBuilder();
            WebBuilder.ConfigureServices(s => s.AddRouting());
            WebBuilder.Configure(app => {
                var routeHandler = new RouteHandler(ctx => {
                    var _podeContext = new PodeContext(ctx, this);
                    this.AddContext(_podeContext);
                    return _podeContext.Start();
                });

                var routeBuilder = new RouteBuilder(app, routeHandler);
                routeBuilder.MapRoute("pode sub-routes", "{*.}");

                var routes = routeBuilder.Build();
                app.UseRouter(routes);
            });

            Sockets = new List<PodeSocket>();
            Contexts = new BlockingCollection<PodeContext>();
        }

        public void Add(PodeSocket socket)
        {
            // if this socket has a hostname, try to re-use an existing socket
            if (socket.HasHostnames)
            {
                var foundSocket = Sockets.FirstOrDefault(x => x.Equals(socket));
                if (foundSocket != default(PodeSocket))
                {
                    foundSocket.Hostnames.AddRange(socket.Hostnames);
                    return;
                }
            }

            socket.BindListener(this);
            Sockets.Add(socket);
        }

        public PodeContext GetContext(CancellationToken cancellationToken)
        {
            return Contexts.Take(cancellationToken);
        }

        public Task<PodeContext> GetContextAsync(CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() => GetContext(cancellationToken), cancellationToken);
        }

        public void AddContext(PodeContext context)
        {
            lock (Contexts)
            {
                Contexts.Add(context);
            }
        }

        public void Start()
        {
            WebBuilder.UseKestrel((options) => {
                foreach (var socket in Sockets)
                {
                    socket.Listen(options);
                }
            });

            WebHost = WebBuilder.Build();
            WebHost.RunAsync(CancellationToken);
            IsListening = true;
        }

        public PodeSocket FindSocket(IPEndPoint ipEndpoint)
        {
            return Sockets.FirstOrDefault(x => x.Equals(ipEndpoint));
        }

        public void Dispose()
        {
            IsListening = false;
            WebHost.Dispose();
            IsDisposed = true;
        }
    }
}