using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shopify_Product_Loader
{
    public class Product
    {
        public string Handle { get; set; }
        public string Title { get; set; }
        public string BodyHTML { get; set; }
        public string Vendor { get; set; }
        public string Type { get; set; }
        public string Tags { get; set; }
        public string Published { get; set; }
        public string Image_Src { get; set; }
        public string Variant_Image { get; set; }
        public string EAN { get; set; }
        public string ProductType { get; set; }
        public List<ProductVariant> ProductVariantList { get; set; }
        public List<string> Other_Images { get; set; }
        public List<string> Size { get; set; }
        public string Ex_Shopify_Id { get; set; }
        public string itemGroupId { get; set; }
        public List<Option> Options { get; set; }
        public bool hasProcessed { get; set; }
    }

    public class ProductVariant
    {
        public List<string> Options { get; set; }
        public string Price { get; set; }
        public string barcode { get; set; }
        public string Ex_Shopify_Id { get; set; }
        public string Title { get; set; }
    }



    public class Option
    {
        public string Name { get; set; }
        public List<string> OptionValue { get; set; }

    }
}
