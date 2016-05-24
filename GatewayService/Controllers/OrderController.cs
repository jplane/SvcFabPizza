
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using OrderService.Interfaces;

namespace GatewayService.Controllers
{
    public class OrderController : ApiController
    {
        public async Task<IHttpActionResult> Post()
        {
            var data = await Request.Content.ReadAsFormDataAsync();

            var details = GetDetails(data["text"], data["trigger_word"]);

            var actorSvcUri = ConfigurationManager.AppSettings["actorSvcUri"];

            var orderProxy = ActorProxy.Create<IPizzaOrder>(ActorId.CreateRandom(), new Uri(actorSvcUri));

            await orderProxy.SetDetailsAsync(details);

            return Json(new
            {
                text = $"order {orderProxy.GetActorId().GetLongId()} created"
            });
        }

        private static OrderDetails GetDetails(string text, string trigger)
        {
            text = text.Replace(trigger, string.Empty).Trim();

            var parts = text.Split(',').ToDictionary(part => part.Split('=')[0].Trim(), part => part.Split('=')[1].Trim());

            var details = OrderDetails.Default;

            if (parts.ContainsKey("toppings"))
            {
                var toppings = parts["toppings"].Split('|');

                details.Toppings = toppings.Aggregate((Toppings)(-1), (accum, next) =>
                {
                    var t = (Toppings)Enum.Parse(typeof(Toppings), next, true);

                    if (accum == (Toppings)(-1))
                    {
                        return t;
                    }
                    else
                    {
                        return accum | t;
                    }
                });
            }

            if (parts.ContainsKey("extracheese"))
            {
                details.ExtraCheese = bool.Parse(parts["extracheese"]);
            }

            if (parts.ContainsKey("size"))
            {
                details.Size = (Size)Enum.Parse(typeof(Size), parts["size"]);
            }

            if (parts.ContainsKey("address"))
            {
                details.Address = parts["address"];
            }

            return details;
        }
    }
}
