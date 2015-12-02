using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shopify_Product_Loader
{
    class Program
    {
        static void Main(string[] args)
        {
            int Product_Updates = 0;

            int Product_Create = 0;

            int Product_OutOfStock = 0;

            try
            {
                List<Product> product = new List<Product>();

                ShopifyAPI shopfiyAPI = new ShopifyAPI();

                Simple_Feed feed = new Simple_Feed(shopfiyAPI,System.Configuration.ConfigurationManager.AppSettings["Source_CSV_Location"]);

                bool ok = feed.Run();

                if (ok)
                {
                    product.AddRange(product);
                    Product_Create = feed.Product_Create;
                    Product_Updates = feed.Product_Updates;
                    Product_OutOfStock = feed.Product_OutStock;
                }
            }
            catch (Exception ex)
            {
                Error(ex);
            }

            Console.WriteLine("Product Updates: " + Product_Updates);

            Console.WriteLine("Product Create:  " + Product_Create);

            Console.WriteLine("Product Out of Stock: " + Product_OutOfStock);

            Console.WriteLine("Done - " + DateTime.Now.ToLongTimeString());

        }

        static void Error(Exception ex)
        {
            if (File.Exists(ConfigurationManager.AppSettings["Error"]) == true)
            {
                File.Delete(ConfigurationManager.AppSettings["Error"]);
            }

            using (FileStream file = File.Create(ConfigurationManager.AppSettings["Error"]))
            {
                using (StreamWriter sw = new StreamWriter(file))
                {
                    if (ex.GetType() == typeof(WebException))
                    {
                        sw.Write(((WebException)ex).ToString());
                        System.Console.WriteLine(((WebException)ex).ToString());
                    }
                    else
                    {
                        sw.Write(ex.ToString());
                        System.Console.WriteLine(ex.ToString());
                    }
                    sw.Close();
                }
                file.Close();

            }

        }

    }
}
