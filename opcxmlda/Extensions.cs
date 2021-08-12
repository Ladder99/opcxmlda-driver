using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace l99.driver.opcxmlda
{
    public static class Extensions
    {
        public static bool IsDifferentHash(this IEnumerable<dynamic> one, IEnumerable<dynamic> two)
        { 
            var one_hc = one.Select(x => x.GetHashCode());
            var two_hc = two.Select(x => x.GetHashCode());

            if (one_hc.Except(two_hc).Count() + two_hc.Except(one_hc).Count() > 0)
                return true;

            return false;
        }

        public static bool IsDifferentString(this object one, object two)
        {
            return !JObject.FromObject(one).ToString().Equals(JObject.FromObject(two).ToString());
        }
    }
}