using System;
using System.Threading.Tasks;
using l99.driver.@base;
using Newtonsoft.Json.Linq;

namespace l99.driver.opcxmlda.handlers
{
    public class SplunkMetric: Handler
    {
        private int _counter = 0;
        
        public SplunkMetric(Machine machine) : base(machine)
        {
            
        }

        public override async Task<dynamic?> OnDataChangeAsync(Veneers veneers, Veneer veneer, dynamic? beforeChange)
        {
            var payload = new
            {
                time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
                @event = "metric",
                host = veneers.Machine.Id,
                fields = new
                {
                    _value = veneer.LastArrivedValue.value,
                    metric_name = veneer.LastArrivedValue.name,
                    metric_type = veneer.LastArrivedValue.type
                }
            };
                
            Console.WriteLine(
                _counter++ + " > " +
                JObject.FromObject(payload).ToString()
            );

            return payload;
        }
        
        protected override async Task afterDataChangeAsync(Veneers veneers, Veneer veneer, dynamic? onChange)
        {
            if (onChange == null)
                return;
            
            var topic = $"opcdaxml/{veneers.Machine.Id}/splunk/{veneer.Name}";
            string payload = JObject.FromObject(onChange).ToString();
            await veneers.Machine.Broker.PublishChangeAsync(topic, payload);
        }
        
        protected override async Task afterDataErrorAsync(Veneers veneers, Veneer veneer, dynamic? onError)
        {
            /*
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new
            {
                method = veneer.LastArrivedInput.method, rc = veneer.LastArrivedInput.rc
            });
            */
        }
    }
}