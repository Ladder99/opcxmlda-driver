using System.Threading.Tasks;
using l99.driver.@base;
using Newtonsoft.Json.Linq;

namespace l99.driver.opcxmlda.veneers
{
    public class Tag : Veneer
    {
        public Tag(string name = "", bool isCompound = false, bool isInternal = false) : base(name, isCompound, isInternal)
        {
            lastChangedValue = new
            {
                name = string.Empty,
                type = string.Empty,
                value = string.Empty
            };
        }
        
        protected override async Task<dynamic> AnyAsync(dynamic input, params dynamic?[] additionalInputs)
        {
            string[] tag_descriptor_name_type = additionalInputs[0].Split(' ');

            var current_value = new
            {
                name = tag_descriptor_name_type[0],
                type = tag_descriptor_name_type.Length > 1 ? tag_descriptor_name_type[1] : string.Empty,
                value = input.Vtq?.Value
            };
            
            await onDataArrivedAsync(input, current_value);
            
            if (!JObject.FromObject(current_value).ToString().Equals(JObject.FromObject(lastChangedValue).ToString()))
            {
                await onDataChangedAsync(input, current_value);
            }
            
            return new { veneer = this };
        }
    }
}