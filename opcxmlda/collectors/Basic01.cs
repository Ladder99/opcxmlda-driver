using System;
using System.Threading.Tasks;
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
                int counter = 0;
                foreach (string descriptor in machine["data"])
                {
                    machine.ApplyVeneer(typeof(opcxmlda.veneers.Tag), 
                        $"{descriptor.Split(' ')[0]}__{counter}");
                    counter++;
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
                    var descriptor = machine["data"][i];
                    var tag = tags.response.read_multiple_tags.results[i];
                    await machine.PeelVeneerAsync($"{descriptor.Split(' ')[0]}__{i}", tag, descriptor);
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