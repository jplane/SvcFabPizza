
using System;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Actors.Runtime;
using OrderService.Interfaces;

namespace OrderService
{
    [StatePersistence(StatePersistence.Persisted)]
    internal class PizzaOrder : Actor, IPizzaOrder
    {
        private IActorTimer _timer = null;

        private async Task CheckStatus(object state)
        {
            var nextChange = await this.StateManager.GetStateAsync<DateTime>("__nextStateChange");

            if (DateTime.UtcNow > nextChange)
            {
                var status = await GetStatusAsync();

                var nextStatus = (Status)(-1);

                switch (status)
                {
                    case Status.Submitted:
                        nextStatus = Status.InPrep;
                        break;

                    case Status.InPrep:
                        nextStatus = Status.Ready;
                        break;

                    case Status.Ready:
                        nextStatus = Status.OutForDelivery;
                        break;

                    case Status.OutForDelivery:
                        nextStatus = Status.Delivered;
                        break;

                    case Status.Delivered:
                        return;
                }

                await UpdateStatus(nextStatus);
            }
        }

        private void TurnOffTimer()
        {
            if (_timer != null)
            {
                this.UnregisterTimer(_timer);
                _timer = null;
            }
        }

        private async Task UpdateStatus(Status nextStatus)
        {
            if (nextStatus == Status.Delivered)
            {
                TurnOffTimer();
            }
            else
            {
                var interval = new Random(Environment.TickCount).Next(10, 20);
                await this.StateManager.SetStateAsync("__nextStateChange", DateTime.UtcNow.AddSeconds(interval));
            }

            await this.StateManager.SetStateAsync("__status", nextStatus);

            await this.SaveStateAsync();

            var msg = new BrokeredMessage(await this.StateManager.GetStateAsync<OrderDetails>("__details"));

            msg.Label = "statusChange";

            msg.Properties["status"] = nextStatus.ToString();
            msg.Properties["id"] = this.Id.ToString();

            var uri = ConfigurationManager.AppSettings["sbUri"];
            var keyName = ConfigurationManager.AppSettings["sbKeyName"];
            var key = ConfigurationManager.AppSettings["sbKey"];
            var topic = ConfigurationManager.AppSettings["sbTopic"];

            var factory = await MessagingFactory.CreateAsync(uri, TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key));

            var sender = await factory.CreateMessageSenderAsync(topic);

            await sender.SendAsync(msg);
        }

        protected override async Task OnActivateAsync()
        {
            var interval = new Random(Environment.TickCount).Next(10, 20);

            await this.StateManager.SetStateAsync("__nextStateChange", DateTime.UtcNow.AddSeconds(interval));

            await this.SaveStateAsync();

            _timer = this.RegisterTimer(CheckStatus, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

            await base.OnActivateAsync();
        }

        protected override Task OnDeactivateAsync()
        {
            TurnOffTimer();
            return base.OnDeactivateAsync();
        }

        public Task<Status> GetStatusAsync()
        {
            return this.StateManager.GetOrAddStateAsync("__status", Status.Submitted);
        }

        public Task<OrderDetails> GetDetailsAsync()
        {
            return this.StateManager.GetOrAddStateAsync("__details", OrderDetails.Default);
        }

        public async Task SetDetailsAsync(OrderDetails value)
        {
            await this.StateManager.SetStateAsync("__details", value);
            await this.SaveStateAsync();
        }
    }
}
