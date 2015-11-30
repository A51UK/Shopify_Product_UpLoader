using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shopify_Product_Loader
{
    public class HeaderType
    {
        public string HeaderName { get; set; }
        public string CSV_HeaderName { get; set; }
        public int ColumnNumber { get; set; }
        public bool IsVariant { get; set; }
    }
}
