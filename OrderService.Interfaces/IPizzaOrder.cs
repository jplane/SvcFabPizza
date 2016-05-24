using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace OrderService.Interfaces
{
    public interface IPizzaOrder : IActor
    {
        Task<OrderDetails> GetDetailsAsync();
        Task SetDetailsAsync(OrderDetails value);
        Task<Status> GetStatusAsync();
    }

    [DataContract]
    public class OrderDetails
    {
        [DataMember]
        public Toppings Toppings { get; set; }
        [DataMember]
        public bool ExtraCheese { get; set; }
        [DataMember]
        public Size Size { get; set; }
        [DataMember]
        public string Address { get; set; }

        public static OrderDetails Default
        {
            get
            {
                return new OrderDetails
                {
                    Toppings = Toppings.Cheese | Toppings.Pepperoni,
                    ExtraCheese = false,
                    Size = Size.Small,
                    Address = "Atlanta, GA"
                };
            }
        }
    }

    public enum Status
    {
        Submitted = 1,
        InPrep,
        Ready,
        OutForDelivery,
        Delivered
    }

    public enum Size
    {
        Small = 1,
        Medium,
        Large,
        ExtraLarge,
        YouMustBeJoking
    }

    [Flags]
    public enum Toppings
    {
        Cheese = 1,
        Pepperoni = 2,
        Mushrooms = 4,
        Sausage = 8,
        Anchovies = 16
    }
}
