//------------------------------------------------------------------------------
// <auto-generated>
//    Этот код был создан из шаблона.
//
//    Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//    Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ExelConverter.Core.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class convertion_rules
    {
        public int id { get; set; }
        public int fk_operator_id { get; set; }
        public string convertion_rule { get; set; }
        public byte[] convertion_rule_image { get; set; }
    }
}
