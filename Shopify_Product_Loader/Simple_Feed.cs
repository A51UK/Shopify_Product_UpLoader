using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shopify_Product_Loader
{
    public class Simple_Feed : FeedBase
    {

        private List<Product> outOfStock;

        public List<Product> OutOfStock
        {
            get
            {
                if (outOfStock == null)
                {
                    outOfStock = new List<Product>();
                }
                return outOfStock;
            }
        }


        public Simple_Feed(ShopifyAPI _shopifyAPI) : base(_shopifyAPI)
        {

        }

        public Simple_Feed(ShopifyAPI _shopifyAPI, string _download_location) : base(_shopifyAPI, _download_location)
        {

        }

        public override bool Run()
        {
            bool done = false;

            try
            {
                Load_Supplier_Data();

                Console.WriteLine("SUPPLIER DATA ...");

                ProcessProduct();

                Console.WriteLine("PRODUCTS PROCESSING DONE..");

                done = Output_Product_API(0);

                shopifyAPI.CheckProductOutStock_API(OutOfStock);
            }

            catch (Exception ex)
            {

                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }

            return done;

        }


        public override void ProcessProduct()
        {
           foreach(var row in Source_CSV)
           {
                if (row[headerType.Where(h => h.HeaderName == "InStock").SingleOrDefault().ColumnNumber].ToLower() == "true")
                {
                    Product _product = new Product();
                    
                    _product.Handle = row[headerType.Where(h => h.HeaderName == "Handle").SingleOrDefault().ColumnNumber];
                    _product.Title = row[headerType.Where(h => h.HeaderName == "Title").SingleOrDefault().ColumnNumber];
                    _product.BodyHTML = row[headerType.Where(h => h.HeaderName == "BodyHTML").SingleOrDefault().ColumnNumber];
                    _product.EAN = row[headerType.Where(h => h.HeaderName == "Barcode").SingleOrDefault().ColumnNumber];
                    _product.Vendor = row[headerType.Where(h => h.HeaderName == "Vendor").SingleOrDefault().ColumnNumber];
                    _product.Type = row[headerType.Where(h => h.HeaderName == "Type").SingleOrDefault().ColumnNumber];
                    _product = GetImages(_product, row);
                    _product = GetOptions(_product, row);
                    _product = GetVariant(_product, row);
                    _products.Add(_product);
                }
                else
                {
                    Product _product = new Product();
                    _product.Handle = row[headerType.Where(h => h.HeaderName == "Handle").SingleOrDefault().ColumnNumber];
                    OutOfStock.Add(_product);
                    Product_OutStock = Product_OutStock + 1;
                    _products.Add(_product);
                }
            }

        }

        protected override Product GetImages(Product _product, List<string> row)
        {
            _product.Other_Images = new List<string>();

            _product.Image_Src = row[headerType.Where(h => h.HeaderName == "Image_1").SingleOrDefault().ColumnNumber];
            _product.Other_Images.Add(row[headerType.Where(h => h.HeaderName == "Image_2").SingleOrDefault().ColumnNumber]);
            _product.Other_Images.Add(row[headerType.Where(h => h.HeaderName == "Image_3").SingleOrDefault().ColumnNumber]);

            return _product;
        }

        protected override Product GetOptions(Product _product, List<string> row)
        {
            string optionList = System.Configuration.ConfigurationManager.AppSettings["Variant_Column"];

            _product.Options = new List<Option>();

            if (optionList.Count() > 0)
            {
                List<string> optionToAdd = optionList.Split(',').ToList();

                foreach (var lOptions in optionToAdd)
                {

                    Option _options = new Option();

                    _options.OptionValue = new List<string>();

                    _options.Name = lOptions;

                    List<string> optionValues = row[headerType.Where(h => h.HeaderName == lOptions).SingleOrDefault().ColumnNumber].Split(System.Configuration.ConfigurationSettings.AppSettings["Variant_Separator"][0]).ToList();

                    foreach (var _value in optionValues)
                    {
                        _options.OptionValue.Add(_value);
                    }

                    _product.Options.Add(_options);
                }
            }

            return _product;
        }

        protected override Product GetVariant(Product _product, List<string> row)
        {
            _product.ProductVariantList = new List<ProductVariant>();

            if(_product.Options.Count() == 0)
            {
                ProductVariant variant = new ProductVariant();
                variant.Price = Working_Sale_Price(row[headerType.Where(h => h.HeaderName == "Price").SingleOrDefault().ColumnNumber],row[headerType.Where(h => h.HeaderName == "DeliveryCost").SingleOrDefault().ColumnNumber]);
                variant.barcode = _product.EAN;
                _product.ProductVariantList.Add(variant);

            }
            else
            {
               var optionOrder =  _product.Options.OrderByDescending(s => s.OptionValue.Count);

               var option_1 = optionOrder.Take(1).SingleOrDefault();

                foreach (var orderValue in option_1.OptionValue)
                {
                    switch (_product.Options.Count())
                    {
                        case 1:
                            ProductVariant variant = new ProductVariant();
                            variant.Price = Working_Sale_Price(row[headerType.Where(h => h.HeaderName == "Price").SingleOrDefault().ColumnNumber], row[headerType.Where(h => h.HeaderName == "DeliveryCost").SingleOrDefault().ColumnNumber]);
                            variant.barcode = _product.EAN;
                            variant.Options = new List<string>();
                            variant.Title = option_1.Name;
                            variant.Options.Add(orderValue);
                            _product.ProductVariantList.Add(variant);
                            break;
                        case 2:
                            var option_2 = optionOrder.Skip(1).Take(1).SingleOrDefault();
                            foreach (var optionOrder_2 in option_2.OptionValue)
                            {
                                variant = new ProductVariant();
                                variant.Price = Working_Sale_Price(row[headerType.Where(h => h.HeaderName == "Price").SingleOrDefault().ColumnNumber], row[headerType.Where(h => h.HeaderName == "DeliveryCost").SingleOrDefault().ColumnNumber]);
                                variant.barcode = _product.EAN;
                                variant.Options = new List<string>();
                                variant.Title = option_1.Name + "_" + option_2.Name;
                                variant.Options.Add(orderValue);
                                variant.Options.Add(optionOrder_2);
                                _product.ProductVariantList.Add(variant);

                            }
                            break;
                        case 3:
                            option_2 = optionOrder.Skip(1).Take(1).SingleOrDefault();
                            foreach (var optionOrder_2 in option_2.OptionValue)
                            {
                                var option_3 = optionOrder.Skip(2).Take(1).SingleOrDefault();
                                foreach (var optionOrder_3 in option_3.OptionValue)
                                {
                                    variant = new ProductVariant();
                                    variant.Price = Working_Sale_Price(row[headerType.Where(h => h.HeaderName == "Price").SingleOrDefault().ColumnNumber], row[headerType.Where(h => h.HeaderName == "DeliveryCost").SingleOrDefault().ColumnNumber]);
                                    variant.barcode = _product.EAN;
                                    variant.Options = new List<string>();
                                    variant.Title = option_1.Name + "_" + option_2.Name + "_" + option_3.Name;
                                    variant.Options.Add(orderValue);
                                    variant.Options.Add(optionOrder_2);
                                    variant.Options.Add(optionOrder_3);
                                    _product.ProductVariantList.Add(variant);
                                }
                            }
                            break;
                    }

                }

            }

            return _product;
        }
    }
}
