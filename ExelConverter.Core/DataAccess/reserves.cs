//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ExelConverter.Core.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class reserves
    {
        public decimal id { get; set; }
        public long user_id { get; set; }
        public int order_id { get; set; }
        public long subuser_id { get; set; }
        public long resource_id { get; set; }
        public System.DateTime period { get; set; }
        public string status { get; set; }
        public decimal price_nominal { get; set; }
        public decimal price_actual { get; set; }
        public decimal subprice_nominal { get; set; }
        public decimal subprice_actual { get; set; }
        public System.DateTime added { get; set; }
        public System.DateTime expires { get; set; }
        public bool is_notified { get; set; }
        public bool is_active { get; set; }
        public int doer_id { get; set; }
    }
}
