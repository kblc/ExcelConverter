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
    
    public partial class area
    {
        public area()
        {
            this.cities = new HashSet<cities>();
        }
    
        public int id { get; set; }
        public byte countryId { get; set; }
        public string caption { get; set; }
        public short sort { get; set; }
    
        public virtual countries countries { get; set; }
        public virtual ICollection<cities> cities { get; set; }
    }
}
