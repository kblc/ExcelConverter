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
    
    public partial class presentation_link_resource
    {
        public long id { get; set; }
        public long linkId { get; set; }
        public long resourceId { get; set; }
    
        public virtual presentation_link presentation_link { get; set; }
        public virtual resources resources { get; set; }
    }
}
