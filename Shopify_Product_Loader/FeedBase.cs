using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Shopify_Product_Loader
{
    public class FeedBase
    {
        protected Exception Error_Exception = null;

        protected List<HeaderType> headerType = new List<HeaderType>();

        protected string error_Response = string.Empty;

        protected string download_location = string.Empty;

        protected List<List<string>> Source_CSV = new List<List<string>>();

        protected List<Product> _products = new List<Product>();

        protected ShopifyAPI shopifyAPI;

        public int Product_Updates = 0;
        public int Product_Create = 0;

        public int Product_OutStock = 0;

        public FeedBase(ShopifyAPI _shopifyAPI, string _download_location)
        {
            shopifyAPI = _shopifyAPI;
            download_location = _download_location;
        }

        public FeedBase(ShopifyAPI _shopifyAPI)
        {
            shopifyAPI = _shopifyAPI;
            download_location = "";
        }

        public virtual void Load_Supplier_Data()
        {
            int loop = 0;

            Char csvSeparator = System.Configuration.ConfigurationManager.AppSettings["Input_CSV_Separator"][0];

            string[] output_Header =  System.Configuration.ConfigurationManager.AppSettings["OutPut_Header_Name"].Split(',');
             
            string[] input_CSV_Header = System.Configuration.ConfigurationManager.AppSettings["Input_CSV_Header"].Split(',');

            List<string> variant_Column = System.Configuration.ConfigurationManager.AppSettings["Variant_Column"].Split(',').ToList();

            for(int i = 0; i < output_Header.Count(); i++)
            {
                HeaderType _hType = new HeaderType();

                _hType.HeaderName = output_Header[i]; 

                if(input_CSV_Header.Count() > i)
                {
                    _hType.CSV_HeaderName = input_CSV_Header[i];

                    if(variant_Column.Where(v => v == _hType.CSV_HeaderName).SingleOrDefault() != null)
                    {
                        _hType.IsVariant = true;
                    }
                    headerType.Add(_hType);
                }
            }


            foreach (string line in File.ReadAllLines(download_location))
            {
                int columnNumber = 0; 

                List<string> _line = new List<string>();

                if (loop == 0)
                {
                    List<string> header_line = new List<string>();

                    header_line = line.Split(csvSeparator).ToList();

                    foreach (string header in header_line)
                    {
                        var headerTypeTemp = headerType.Where(h => h.CSV_HeaderName == header).SingleOrDefault();

                        if (headerTypeTemp != null)
                        {
                            headerTypeTemp.ColumnNumber = columnNumber;
                        }

                        columnNumber = columnNumber + 1;
                    }

                }
                else
                {
                    _line = line.Split(csvSeparator).ToList();
                }

                if (_line.Count > 0)
                {
                    Source_CSV.Add(_line);
                }
                loop = loop + 1;
            }
        }

        public virtual void ProcessProduct()
        {
            throw new NotImplementedException();
        }

        protected virtual Product GetOptions(Product _product, List<string> row)
        {
            throw new NotImplementedException();
        }

        protected virtual Product GetVariant(Product _product, List<string> row)
        {
            throw new NotImplementedException();
        }

        protected virtual Product GetImages(Product _product, List<string> row)
        {
            throw new NotImplementedException();
        }


        public virtual bool Run()
        {
            throw new NotImplementedException();
        }

        protected virtual string Tags()
        {
            throw new NotImplementedException();
        }

        protected virtual string Working_Sale_Price(string price, string delivery)
        {
            //Add a mark up by a % to the price, then round it to 2 decimal places and then turn into  a string.

            string priceTemp = (Math.Round((decimal.Parse(price) + decimal.Parse(delivery)) + ((decimal.Parse(price) + decimal.Parse(delivery)) * int.Parse(System.Configuration.ConfigurationManager.AppSettings["MarkUp"]) / 100), 2)).ToString();

            //Round up the price to .99. (e.g. 4.62 -> 4.99 || 0.23 -> 0.99);

            if (priceTemp.Contains('.'))
            {
                //Remove all text start from the index of '.' (e.g. 3.45 -> 3)
                priceTemp = priceTemp.Remove(priceTemp.IndexOf('.'));

                //Add the '.99' to the price string. (e.g. 3 -> 3.99)
                priceTemp = priceTemp + ".99";
            }
            return priceTemp;

        }

        protected virtual bool Output_Product_API(int retryAttempt)
        {



            try
            {
                var groupProduct = _products.Where(p => p.hasProcessed == false).GroupBy(s => s.Handle).ToList();

                int cnt = groupProduct.Count();


                foreach (var group in groupProduct)
                {
                    Console.WriteLine("Total Number of Products waiting to processed are: " + cnt);
                    Console.WriteLine("*********************************************************");
                    var productsList = group.ToList();
                    var product = productsList[0];


                    System.Collections.Specialized.NameValueCollection collection = new System.Collections.Specialized.NameValueCollection();
                    collection.Add("handle", product.Handle);
                    collection.Add("limit", "1");

                    //Check if the product is in shopify.

                    var shopifyProduct = shopifyAPI.Shopify_Get("/admin/products.json", collection);


                    Shopify_Product_Loader_ShopifyClass.RootObject jsonObject = null;

                    try
                    {

                        JavaScriptSerializer json = new JavaScriptSerializer();

                        jsonObject = json.Deserialize<Shopify_Product_Loader_ShopifyClass.RootObject>((string)shopifyProduct);
                    }
                    catch (Exception ex)
                    {

                        int f = 3;
                    }

                    // if new create product else update it. 


                    if (jsonObject != null && jsonObject.products != null && jsonObject.products.Count > 0)
                    {
                        Product_Updates = Product_Updates + 1;
                        shopifyAPI.UpdateProduct(product, jsonObject.products[0].id.ToString(), jsonObject.products[0]);
                    }
                    else
                    {
                        Product_Create = Product_Create + 1;
                        shopifyAPI.CreateNewProduct(product);
                    }


                    product.hasProcessed = true;

                    Thread.Sleep(new TimeSpan(0, 0, 1));
                    cnt--;

                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                if (ex.GetType() == typeof(WebException))
                {
                    if (retryAttempt <= 5)
                    {
                        Thread.Sleep(new TimeSpan(0, 2, 0));
                        Console.WriteLine("I am retrying and my retry number is : " + retryAttempt);
                        Output_Product_API(retryAttempt++);
                    }
                }

                Error_Exception = ex;
                Error(ex);
                return false;
            }


        }

        private void Error(Exception ex)
        {

            if (File.Exists(System.Configuration.ConfigurationManager.AppSettings["Error"]) == true)
            {
                File.Delete(System.Configuration.ConfigurationManager.AppSettings["Error"]);
            }

            using (FileStream file = File.Create(System.Configuration.ConfigurationManager.AppSettings["Error"]))
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

        public List<Product> GetProduct
        {
            get
            {
                return _products;
            }

        }
    }
}
