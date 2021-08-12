using System.Threading.Tasks;
using l99.driver.@base;

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
                value = string.Empty,
                good = false
            };
        }
        
        protected override async Task<dynamic> AnyAsync(dynamic input, params dynamic?[] additionalInputs)
        {
            var current_value = new
            {
                name = additionalInputs[0],
                type = input.Vtq?.ValueType.Name,
                value = input.Vtq?.Value,
                good = input.Vtq?.Quality.IsGood
            };
            
            await onDataArrivedAsync(input, current_value);
            
            if (current_value.IsDifferentString((object)lastChangedValue))
            {
                await onDataChangedAsync(input, current_value);
            }
            
            return new { veneer = this };
        }
    }
}