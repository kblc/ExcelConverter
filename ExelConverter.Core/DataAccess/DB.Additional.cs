using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExelConverter.Core.DataAccess
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.EntityClient;

    public partial class alphaEntities : DbContext
    {
        private static string entityConnectionString = string.Empty;
        public static string EntityConnectionString
        {
            get
            {
                return entityConnectionString;
            }
            set
            {
                if (value == entityConnectionString)
                    return;
                entityConnectionString = value;
                EntityConnectionStringBuilder csb = new EntityConnectionStringBuilder(entityConnectionString);
                ProviderConnectionString = csb.ProviderConnectionString;
            }
        }

        private static string providerConnectionString = string.Empty;
        public static string ProviderConnectionString
        {
            get
            {
                return providerConnectionString;
            }
            set
            {
                if (value == providerConnectionString)
                    return;
                providerConnectionString = value;
                EntityConnectionStringBuilder builder = new EntityConnectionStringBuilder()
                {
                    Metadata = "res://*/DataAccess.AppDB.csdl|res://*/DataAccess.AppDB.ssdl|res://*/DataAccess.AppDB.msl",
                    Provider = "MySql.Data.MySqlClient",
                    ProviderConnectionString = providerConnectionString
                };
                EntityConnectionString = builder.ConnectionString+"";
            }
        }

        public alphaEntities(string connectionString): base(connectionString) { }
        public static alphaEntities New()
        {
            if (string.IsNullOrWhiteSpace(EntityConnectionString))
                return new alphaEntities(); else
                return new alphaEntities(EntityConnectionString);
        }
    }

    public partial class exelconverterEntities2 : DbContext
    {
        private static string entityConnectionString = string.Empty;
        public static string EntityConnectionString
        {
            get
            {
                return entityConnectionString;
            }
            set
            {
                if (value == entityConnectionString)
                    return;
                entityConnectionString = value;
                EntityConnectionStringBuilder csb = new EntityConnectionStringBuilder(entityConnectionString);
                ProviderConnectionString = csb.ProviderConnectionString;
            }
        }

        private static string providerConnectionString = string.Empty;
        public static string ProviderConnectionString
        {
            get
            {
                return providerConnectionString;
            }
            set
            {
                if (value == providerConnectionString)
                    return;
                providerConnectionString = value;
                EntityConnectionStringBuilder builder = new EntityConnectionStringBuilder()
                {
                    Metadata = "res://*/DataAccess.ConverterDB.csdl|res://*/DataAccess.ConverterDB.ssdl|res://*/DataAccess.ConverterDB.msl",
                    Provider = "MySql.Data.MySqlClient",
                    ProviderConnectionString = providerConnectionString
                };
                EntityConnectionString = builder.ConnectionString;
            }
        }

        public exelconverterEntities2(string connectionString) : base(connectionString) { }
        public static exelconverterEntities2 New()
        {
            if (string.IsNullOrWhiteSpace(EntityConnectionString))
                return new exelconverterEntities2(); else
                return new exelconverterEntities2(EntityConnectionString);
        }
    }
}
