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
    
    public partial class seo_place
    {
        public long id { get; set; }
        public long typeId { get; set; }
        public Nullable<long> cityId { get; set; }
        public Nullable<long> regionId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string h1 { get; set; }
    
        public virtual cities cities { get; set; }
        public virtual regions regions { get; set; }
        public virtual resource_types resource_types { get; set; }
    }
}
