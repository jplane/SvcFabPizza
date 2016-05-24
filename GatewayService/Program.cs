using System;
using System.Configuration;
using System.Diagnostics;
using System.Fabric;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Services.Runtime;

namespace GatewayService
{
    internal static class Program
    {
        private static Task _stateChanges = null;

        private static void Main()
        {
            try
            {
                ServiceRuntime.RegisterServiceAsync("GatewayServiceType",
                    context => new GatewayService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(GatewayService).Name);

                _stateChanges = Task.Factory.StartNew(WatchForStateChanges, TaskCreationOptions.LongRunning);

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        private static void WatchForStateChanges()
        {
            var uri = ConfigurationManager.AppSettings["sbUri"];
            var keyName = ConfigurationManager.AppSettings["sbKeyName"];
            var key = ConfigurationManager.AppSettings["sbKey"];
            var topic = ConfigurationManager.AppSettings["sbTopic"];
            var subscription = ConfigurationManager.AppSettings["sbSubscription"];

            var slackUri = ConfigurationManager.AppSettings["slackIncomingUri"];

            var factory = MessagingFactory.Create(uri, TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key));

            var sub = factory.CreateSubscriptionClient(topic, subscription);

            while (true)
            {
                var msg = sub.Receive(TimeSpan.FromSeconds(5));

                if (msg != null)
                {
                    try
                    {
                        var text = $"order {msg.Properties["id"]} has new status {msg.Properties["status"]}";

                        var http = new HttpClient();

                        http.PostAsJsonAsync(new Uri(slackUri), new {text}).Wait();

                        msg.Complete();
                    }
                    catch (Exception)
                    {
                        msg.Abandon();
                    }
                }
            }
        }
    }
}
