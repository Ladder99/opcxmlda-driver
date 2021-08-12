using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using OpcLabs.EasyOpc;
using OpcLabs.EasyOpc.DataAccess;
using OpcLabs.EasyOpc.DataAccess.OperationModel;

namespace l99.driver.opcxmlda
{
    public partial class Platform
    {
        public async Task<dynamic> ReadMultipleTagsAsync(List<dynamic> descriptors)
        {
            return await Task.FromResult(ReadMultipleTags(descriptors));
        }
        
        public dynamic ReadMultipleTags(List<dynamic> descriptors)
        {
            DAVtqResult[] results = null;
            
            NativeDispatchReturn ndr = nativeDispatch(() =>
            {
                 results = _machine.Client.ReadMultipleItems(
                    new ServerDescriptor { UrlString = _machine.OpcxmldaEndpoint.URI },
                    Array.ConvertAll(descriptors.ToArray(),
                        new Converter<dynamic, DAItemDescriptor>(item =>
                            new DAItemDescriptor( (string)((Dictionary<object, object>.KeyCollection)item.Keys) .ElementAt(0))
                        )));
                
                return true;
            });
            
            var nr = new
            {
                invocationMs = ndr.ElapsedMilliseconds,
                request = new {read_multiple_tags = new {descriptors}},
                response = new {read_multiple_tags = new {results}}
            };
            
            _logger.Trace($"[{_machine.Id}] Platform invocation result:\n{JObject.FromObject(nr).ToString()}");

            return nr;
        }
    }
}