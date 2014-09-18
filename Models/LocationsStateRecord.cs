﻿using System.ComponentModel.DataAnnotations;

namespace OShop.Models {
    public class LocationsStateRecord {
        public virtual int Id { get; set; }
        public virtual bool Enabled { get; set; }
        [Required]
        public virtual string Name { get; set; }
        [Required]
        public virtual string IsoCode { get; set; }
        public virtual LocationsCountryRecord Country { get; set; }
        public virtual ShippingZoneRecord ShippingZone { get; set; }
    }
}