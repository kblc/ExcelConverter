﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class alphaEntities : DbContext
    {
        public alphaEntities()
            : base("name=alphaEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<area> area { get; set; }
        public DbSet<cart> cart { get; set; }
        public DbSet<cities> cities { get; set; }
        public DbSet<companies> companies { get; set; }
        public DbSet<countries> countries { get; set; }
        public DbSet<discounts_periods> discounts_periods { get; set; }
        public DbSet<discounts_qty> discounts_qty { get; set; }
        public DbSet<help_chapters> help_chapters { get; set; }
        public DbSet<hots> hots { get; set; }
        public DbSet<import_rectangle> import_rectangle { get; set; }
        public DbSet<locations> locations { get; set; }
        public DbSet<managers> managers { get; set; }
        public DbSet<mockup> mockup { get; set; }
        public DbSet<mockup_set> mockup_set { get; set; }
        public DbSet<news> news { get; set; }
        public DbSet<operator_orders> operator_orders { get; set; }
        public DbSet<orders> orders { get; set; }
        public DbSet<orders_confirms> orders_confirms { get; set; }
        public DbSet<pages> pages { get; set; }
        public DbSet<photo_report> photo_report { get; set; }
        public DbSet<photo_report_resource> photo_report_resource { get; set; }
        public DbSet<photo_report_resource_photo> photo_report_resource_photo { get; set; }
        public DbSet<preferences> preferences { get; set; }
        public DbSet<presentation_link> presentation_link { get; set; }
        public DbSet<presentation_link_resource> presentation_link_resource { get; set; }
        public DbSet<regions> regions { get; set; }
        public DbSet<reserves> reserves { get; set; }
        public DbSet<resource_import_log> resource_import_log { get; set; }
        public DbSet<resource_lights> resource_lights { get; set; }
        public DbSet<resource_price_rules> resource_price_rules { get; set; }
        public DbSet<resource_sides> resource_sides { get; set; }
        public DbSet<resource_sizes> resource_sizes { get; set; }
        public DbSet<resource_types> resource_types { get; set; }
        public DbSet<resources> resources { get; set; }
        public DbSet<resources_history> resources_history { get; set; }
        public DbSet<seo_place> seo_place { get; set; }
        public DbSet<static_pages> static_pages { get; set; }
        public DbSet<subresources> subresources { get; set; }
        public DbSet<subresources_history> subresources_history { get; set; }
        public DbSet<sync> sync { get; set; }
        public DbSet<synonym_word> synonym_word { get; set; }
        public DbSet<tooltip> tooltip { get; set; }
        public DbSet<tooltip_category> tooltip_category { get; set; }
        public DbSet<users> users { get; set; }
        public DbSet<users_clients> users_clients { get; set; }
        public DbSet<users_history> users_history { get; set; }
        public DbSet<users_ips> users_ips { get; set; }
    }
}
