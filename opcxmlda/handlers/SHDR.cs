using System.Collections.Generic;
using System.Threading.Tasks;
using l99.driver.@base;
using System.Linq;
using MTConnect;

namespace l99.driver.opcxmlda.handlers
{
    public class SHDR: Handler
    {
        private int _counter = 0;

        private MTConnect.Adapter _adapter;
        
        public SHDR(Machine machine) : base(machine)
        {
            
        }

        public override async Task InitializeAsync(dynamic config)
        {
            _adapter = new Adapter(config["port"], config["verbose"]);
            
            _adapter.AddDataItem(new Event("avail"));
            _adapter.UpdateDataItem("avail", "AVAILABLE");
            
            foreach (Dictionary<object,object> descriptor in machine["data"])
            {
                try
                {
                    string descriptor_id = (string)descriptor.Keys.ElementAt(0);
                    var shdr = ((Dictionary<object, object>)((Dictionary<object, object>)descriptor[descriptor_id])["shdr"]);

                    var di_name = (string)shdr["name"];
                    var di_cat = (string)shdr["category"];

                    switch (di_cat)
                    {
                        case "sample":
                            _adapter.AddDataItem(new Sample(di_name));
                            break;
                        
                        case "event":
                            _adapter.AddDataItem(new Event(di_name));
                            break;
                        
                        case "condition":
                            _adapter.AddDataItem(new Condition(di_name, true));
                            break;
                    }
                }
                catch
                {
                    
                }
            }

            _adapter.Start();
        }

        public override async Task<dynamic?> OnDataChangeAsync(Veneers veneers, Veneer veneer, dynamic? beforeChange)
        {
            _adapter.Begin();

            foreach (Dictionary<object, object> descriptor in machine["data"])
            {
                try
                {
                    string descriptor_id = (string)descriptor.Keys.ElementAt(0);
                    if (descriptor_id.Equals(veneer.Name))
                    {
                        var di_name = (string)((Dictionary<object, object>)((Dictionary<object, object>)descriptor[descriptor_id])["shdr"])["name"];
                        _adapter.UpdateDataItem(di_name, veneer.LastArrivedValue.value);
                        break;
                    }
                }
                catch 
                {
                   
                }
            }

            return null;
        }
        
        protected override async Task afterSweepCompleteAsync(Machine machine, dynamic? onSweepComplete)
        {
            var sb = _adapter.SendChanged();
            
            if (sb.Length == 0)
                return;
            
            var topic = $"opcxmlda/{machine.Id}/shdr";
            string payload = sb.ToString();
            await machine.Broker.PublishChangeAsync(topic, payload);
        }
    }
}