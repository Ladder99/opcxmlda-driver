using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using l99.driver.@base;


namespace l99.driver.opcxmlda.collectors
{
    public class Basic01 : Collector
    {
        public Basic01(Machine machine, int sweepMs = 1000, params dynamic[] additionalParams) : base(machine, sweepMs, additionalParams)
        {
            
        }

        public override async Task<dynamic?> InitializeAsync()
        {
            try
            {
                foreach (dynamic descriptor in machine["data"])
                {
                    //machine.ApplyVeneer(typeof(opcxmlda.veneers.Tag), descriptor);
                    
                    var name = (string)((Dictionary<object, object>.KeyCollection)descriptor.Keys).ElementAt(0);
                    machine.ApplyVeneer(typeof(opcxmlda.veneers.Tag), name);
                }
                
                machine.VeneersApplied = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"[{machine.Id}] Collector initialization failed.");
            }

            return null;
        }
   
        public override async Task<dynamic?> CollectAsync()
        {
            try
            {
                dynamic tags = await machine["platform"].ReadMultipleTagsAsync(machine["data"]);

                for (int i = 0; i < tags.response.read_multiple_tags.results.Length; i++)
                {
                    //string descriptor = machine["data"][i];
                    
                    string descriptor = (string)((Dictionary<object, object>.KeyCollection)machine["data"][i].Keys).ElementAt(0);
                    var tag = tags.response.read_multiple_tags.results[i];
                    await machine.PeelVeneerAsync(descriptor, tag, descriptor);
                }
                
                LastSuccess = true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"[{machine.Id}] Collector sweep failed.");
            }

            return null;
        }
    }
}