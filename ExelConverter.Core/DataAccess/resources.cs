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
    
    public partial class resources
    {
        public resources()
        {
            this.photo_report_resource = new HashSet<photo_report_resource>();
            this.presentation_link_resource = new HashSet<presentation_link_resource>();
        }
    
        public long id { get; set; }
        public string code { get; set; }
        public Nullable<long> codeDoors { get; set; }
        public long company_id { get; set; }
        public long type_id { get; set; }
        public long side_id { get; set; }
        public long size_id { get; set; }
        public bool light { get; set; }
        public bool restricted { get; set; }
        public decimal price { get; set; }
        public long city_id { get; set; }
        public Nullable<long> region_id { get; set; }
        public Nullable<long> location_id { get; set; }
        public string address { get; set; }
        public string description { get; set; }
        public System.DateTime added { get; set; }
        public bool is_active { get; set; }
        public sbyte has_image { get; set; }
        public decimal latitude { get; set; }
        public decimal longitude { get; set; }
        public short marker_rotation { get; set; }
        public string approved { get; set; }
        public string rotation_approved { get; set; }
    
        public virtual ICollection<photo_report_resource> photo_report_resource { get; set; }
        public virtual ICollection<presentation_link_resource> presentation_link_resource { get; set; }
    }
}
