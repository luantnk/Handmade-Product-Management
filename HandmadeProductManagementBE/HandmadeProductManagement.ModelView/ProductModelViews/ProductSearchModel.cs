﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandmadeProductManagement.ModelViews.ProductModelViews
{
    public class ProductSearchModel
    {
        public string? Name { get; set; }
        public string? CategoryId { get; set; }
        public string? ShopId { get; set; }
        public string? Status { get; set; }
        public int? MinRating { get; set; }

    }
}
