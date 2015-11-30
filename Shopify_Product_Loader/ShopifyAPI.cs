using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Shopify_Product_Loader
{
    public class ShopifyAPI
    {
        private List<Product> _products = new List<Product>();

        private List<Shopify_Product_Loader_ShopifyClass.Product> productStock = new List<Shopify_Product_Loader_ShopifyClass.Product>();

        public ShopifyAPI()
        {
        }

        public void Load_Stock_Products()
        {
            bool downloadOk = true;

            int i = 0;

            while (downloadOk == true)
            {
                var shopifyProducts = Shopify_Get("/admin/products.json?limit=250&published_status=published&page=" + i);

                if (shopifyProducts != "{\"products\":[]}")
                {

                    Shopify_Product_Loader_ShopifyClass.RootObject jsonObject = null;

                    try
                    {
                        System.Web.Script.Serialization.JavaScriptSerializer json = new System.Web.Script.Serialization.JavaScriptSerializer();

                        jsonObject = json.Deserialize<Shopify_Product_Loader_ShopifyClass.RootObject>((string)shopifyProducts);

                        foreach (var item in jsonObject.products)
                        {
                            productStock.Add(item);
                        }

                        Thread.Sleep(new TimeSpan(0, 0, 1));
                    }
                    catch (Exception ex)
                    {
                        downloadOk = false;
                    }
                }
                else
                {
                    downloadOk = false;
                }
                i = i + 1;
            }
        }

        public void CreateNewProduct(Product product)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{ \"product\": {");
            sb.Append("\"title\":\"" + product.Title + "\"");
            sb.Append(",\"body_html\":\"" + product.BodyHTML.Replace("\t", "").Replace("'", "").Replace("Â", "") + "\"");
            sb.Append(",\"vendor\":\"" + product.Vendor + "\"");
            sb.Append(",\"handle\":\"" + product.Handle + "\"");
            sb.Append(",\"tags\":\"" + product.Tags + "\"");
            sb.Append(",\"product_type\":\"" + product.Type + "\"");

            sb.Append(",\"images\": [ ");

            sb.Append("{\"src\":\"" + product.Image_Src + "\"}");

            //Add more then one images.

            foreach (string image in product.Other_Images)
            {
                sb.Append(",{\"src\":\"" + image + "\"}");
            }

            sb.Append("]");

            if (product.Options != null && product.Options.Count() > 0)
            {

                int optionLoop = 0;
                sb.Append(",\"options\":[");

                foreach (var tempVendor in product.Options)
                {

                    if (optionLoop == 0)
                    {
                        sb.Append("{");

                    }
                    else
                    {
                        sb.Append(",{");
                    }

                    optionLoop = optionLoop + 1;

                    sb.Append("\"name\":\"" + tempVendor.Name + "\"");
                    sb.Append(",\"position\":" + optionLoop);
                    sb.Append(",\"title\":\"" + tempVendor.Name + "\"");

                    int valuesLoop = 0;

                    sb.Append(",\"values\":[");

                    foreach (var tempValue in tempVendor.OptionValue)
                    {
                        if (valuesLoop == 0)
                        {
                            sb.Append("\"" + tempValue + "\"");
                            valuesLoop = valuesLoop + 1;
                        }
                        else
                        {

                            sb.Append(",\"" + tempValue + "\"");
                        }
                    }

                    sb.Append("]");

                    sb.Append("}");
                }

                sb.Append("]");
            }

            sb.Append(",\"variants\" : [");

            int variantsloop = 0;

            if (product.ProductVariantList != null)
            {

                foreach (var vendor in product.ProductVariantList)
                {
                    if (variantsloop == 0)
                    {
                        sb.Append("{");
                        variantsloop = variantsloop + 1;
                    }
                    else
                    {
                        sb.Append(",{");
                    }
                    sb.Append("\"price\":\"" + vendor.Price + "\"");
                    sb.Append(",\"barcode\":\"" + vendor.barcode + "\""); // EAN
                    sb.Append(",\"title\":\"" + vendor.Title + "\"");

                    if (vendor.Options != null)
                    {
                        for (int i = 0; i < vendor.Options.Count() - 1; i++)
                        {
                            sb.Append(",\"option" + i + "\":\"" + vendor.Options + "\"");
                        }
                    }

                    sb.Append("}");
                }
            }

            sb.Append("]");
            sb.Append("}");
            sb.Append("}");

            string json = sb.ToString();

            json = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(json));

            Shopify_Post("/admin/products.json", json, product.Handle);
        }

        //Product with no Variants

        public void UpdateProduct(Product product, string id, string variantId)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{ \"product\": {");
            sb.Append("\"id\":\"" + id + "\"");
            sb.Append(",\"published\":\"true\"");
            //sb.Append(",\"tags\":\"" + product.Tags + "\"");
            sb.Append(",\"variants\": [{");
            sb.Append("\"id\":\"" + variantId + "\"");
            sb.Append(",\"price\":\"" + product.ProductVariantList[0].Price + "\"");
            sb.Append(",\"barcode\":\"" + product.EAN + "\""); // EAN
            sb.Append("}]");
            sb.Append("}");
            sb.Append("}");

            string json = sb.ToString();

            Shopify_Put("/admin/products/" + id + ".json", json, id);

        }

        //Product with Variants. 

        public void UpdateProduct(Product product, string id, Shopify_Product_Loader_ShopifyClass.Product shofiy_product)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{ \"product\": {");
            sb.Append("\"id\":\"" + id + "\"");
            sb.Append(",\"published\":\"true\"");
            //  sb.Append(",\"tags\":\"" + product.Tags + "\"");

            sb.Append(",\"variants\": [");

            int loopVariants = 0;

            foreach (var variants in shofiy_product.variants)
            {
                ProductVariant TempVariant = null;

                if (!string.IsNullOrEmpty(variants.option1) && product.ProductVariantList.Count > 1 && product.Options != null)
                {
                    TempVariant = (from _p in product.ProductVariantList where _p.Options.Where(pp => pp == variants.option1) != null select _p).FirstOrDefault();
                }
                else
                {
                    TempVariant = product.ProductVariantList.FirstOrDefault();
                }

                if (loopVariants == 0)
                {
                    sb.Append("{");
                    loopVariants = loopVariants + 1;
                }
                else
                {
                    sb.Append(",{");
                }
                sb.Append("\"id\":\"" + variants.id + "\"");
                sb.Append(",\"price\":\"" + TempVariant.Price + "\"");
                sb.Append(",\"barcode\":\"" + TempVariant.barcode + "\""); // EAN
                sb.Append("}");
            }

            sb.Append("]");
            sb.Append("}");
            sb.Append("}");

            string json = sb.ToString();

            Shopify_Put("/admin/products/" + id + ".json", json, id);
        }


        //Hide product that are out of stock
        public void UpdateOutStockProduct(string id, string variantId)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("{ \"product\": {");
            sb.Append("\"id\":\"" + id + "\"");
            sb.Append(",\"published\":\"false\"");
            sb.Append("}");
            sb.Append("}");

            string json = sb.ToString();
            Shopify_Put("/admin/products/" + id + ".json", json, id);
        }


        //Use this one when load all the product from shopify.
        public void CheckProductOutStock_PreLoad()
        {
            if (productStock.Count() > 0)
            {
                List<Shopify_Product_Loader_ShopifyClass.Product> outOfStock = new List<Shopify_Product_Loader_ShopifyClass.Product>();

                productStock.ForEach(p =>
                {
                    if (_products.Where(newProduct => newProduct.Handle == p.handle).Count() == 0)
                    {
                        outOfStock.Add(p);
                    }
                });

                if (outOfStock.Count() > 0)
                {
                    int j = outOfStock.Count;
                    foreach (var item in outOfStock)
                    {
                        Console.WriteLine("Total number of products to be marked as hidden :" + j);
                        Console.WriteLine("****************************************************");
                        UpdateOutStockProduct(item.id.ToString(), item.variants[0].id.Value.ToString());

                        Thread.Sleep(new TimeSpan(0, 0, 1));
                        j--;
                    }
                }
            }

        }

        //Use this one when get the data from database
        public void CheckProductOutStock_API(List<Product> products_OutOfStock)
        {
            if (products_OutOfStock.Count() > 0)
            {

                int j = products_OutOfStock.Count();

                foreach (var product in products_OutOfStock)
                {

                    Console.WriteLine("Total number of products to be marked as hidden :" + j);
                    Console.WriteLine("****************************************************");

                    var shopifyProducts = Shopify_Get("/admin/products.json?handle=" + product.Handle);

                    if (shopifyProducts != "{\"products\":[]}")
                    {

                        Shopify_Product_Loader_ShopifyClass.RootObject jsonObject = null;

                        try
                        {
                            System.Web.Script.Serialization.JavaScriptSerializer json = new System.Web.Script.Serialization.JavaScriptSerializer();

                            jsonObject = json.Deserialize<Shopify_Product_Loader_ShopifyClass.RootObject>((string)shopifyProducts);

                            foreach (var item in jsonObject.products)
                            {
                                UpdateOutStockProduct(item.id.ToString(), item.variants[0].id.Value.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            int f = 2;

                        }


                        Thread.Sleep(new TimeSpan(0, 0, 1));
                        j--;
                    }
                }
            }
        }


        public string Shopify_Get(string data, System.Collections.Specialized.NameValueCollection collection)
        {
            WebClient wc = new WebClient();

            string url = "https://" + ConfigurationSettings.AppSettings["Shopify_API_Key"] + ConfigurationSettings.AppSettings["Shopify_Shop_Password"] + ConfigurationSettings.AppSettings["Shopify_Store_Name"] + data;
            int loop = 0;

            foreach (var key in collection.AllKeys)
            {
                if (loop == 0)
                {
                    url = url + "?" + key + "=" + collection[key];
                }
                else
                {
                    url = url + "&" + key + "=" + collection[key];
                }

                loop = loop + 1;
            }

            wc.Credentials = new NetworkCredential(ConfigurationSettings.AppSettings["Shopify_API_Key"], ConfigurationSettings.AppSettings["Shopify_Shop_Password"]);

            string returnData = wc.DownloadString(url);

            return returnData;
        }

        public string Shopify_Get(string data)
        {
            WebClient wc = new WebClient();

            string url = "https://" + ConfigurationManager.AppSettings["Shopify_API_Key"] + ConfigurationManager.AppSettings["Shopify_Shop_Password"] + ConfigurationManager.AppSettings["Shopify_Store_Name"] + data;

            wc.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["Shopify_API_Key"], ConfigurationManager.AppSettings["Shopify_Shop_Password"]);

            string returnData = wc.DownloadString(url);


            return returnData;
        }

        public void Shopify_Put(string hostEnd, string data, string id)
        {
            WebClient wc = new WebClient();
            string url = "https://" + ConfigurationManager.AppSettings["Shopify_API_Key"] + ConfigurationManager.AppSettings["Shopify_Shop_Password"] + ConfigurationManager.AppSettings["Shopify_Store_Name"] + hostEnd;
            wc.Headers["Content-Type"] = "application/json; charset=utf-8";
            wc.Encoding = Encoding.UTF8;
            wc.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["Shopify_API_Key"], ConfigurationManager.AppSettings["Shopify_Shop_Password"]);

            string returnData = wc.UploadString(url, "PUT", data);
        }

        public void Shopify_Post(string hostEnd, string data, string id)
        {
            WebClient wc = new WebClient();

            string url = "https://" + ConfigurationManager.AppSettings["Shopify_API_Key"] + ConfigurationManager.AppSettings["Shopify_Shop_Password"] + ConfigurationManager.AppSettings["Shopify_Store_Name"] + hostEnd;
            wc.Headers["Content-Type"] = "application/json; charset=utf-8";
            wc.Encoding = Encoding.UTF8;
            wc.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["Shopify_API_Key"], ConfigurationManager.AppSettings["Shopify_Shop_Password"]);

            string returnData = wc.UploadString(url, "POST", data);
        }


    }
}
