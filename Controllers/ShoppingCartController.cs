﻿using Orchard.Environment.Extensions;
using Orchard.Environment.Features;
using Orchard.Themes;
using OShop.Models;
using OShop.Services;
using OShop.ViewModels;
using OShop.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Orchard.ContentManagement;

namespace OShop.Controllers
{
    [OrchardFeature("OShop.ShoppingCart")]
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICurrencyProvider _currencyProvider;
        private readonly IEnumerable<IShippingInfoProvider> _shippingInfoProviders;
        private readonly IFeatureManager _featureManager;
        private readonly IContentManager _contentManager;

        private readonly ILocationsService _locationService;
        private readonly IShippingService _shippingService;

        public ShoppingCartController(
            IShoppingCartService shoppingCartService,
            ICurrencyProvider currencyProvider,
            IEnumerable<IShippingInfoProvider> shippingInfoProviders,
            IFeatureManager featureManager,
            IContentManager contentManager,
            ILocationsService locationService = null,
            IShippingService shippingService = null) {
            _shoppingCartService = shoppingCartService;
            _currencyProvider = currencyProvider;
            _shippingInfoProviders = shippingInfoProviders;
            _featureManager = featureManager;
            _contentManager = contentManager;
            _locationService = locationService;
            _shippingService = shippingService;
        }

        [Themed]
        [OutputCache(Duration = 0)]
        public ActionResult Index()
        {
            Location location = new Location(null, null);

            if (_locationService != null) {
                location = GetLocation(
                    _shoppingCartService.GetProperty<int>("CountryId"),
                    _shoppingCartService.GetProperty<int>("StateId")
                );
            };

            var cartItems = _shoppingCartService.ListItems();

            var shippingInfos = _shippingInfoProviders.SelectMany(sip => sip.GetShippingInfos(cartItems));

            var model = new ShoppingCartIndexViewModel() {
                CartItems = cartItems,
                NumberFormat = _currencyProvider.NumberFormat,
                // Optional features
                VatEnabled = _featureManager.GetEnabledFeatures().Where(f => f.Id == "OShop.VAT").Any(),

                ShippingRequired = shippingInfos.IsShippingRequired()
            };

            if (_locationService != null) {
                model.CountryId = location.Country != null ? location.Country.Id : 0;
                model.StateId = location.State != null ? location.State.Id : 0;

                model.Countries = _locationService.GetEnabledCountries();
                model.States = _locationService.GetEnabledStates(model.CountryId);
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult Update(ShoppingCartUpdateViewModel model) {
            if (model.CartItems != null) {
                UpdateCart(model.CartItems);
            }

            if (_locationService != null) {
                var location = GetLocation(model.CountryId, model.StateId);

                if (location.Country != null) {
                    _shoppingCartService.SetProperty("CountryId", location.Country.Id);
                    if (location.State != null) {
                        _shoppingCartService.SetProperty("StateId", location.State.Id);
                    }
                    else {
                        _shoppingCartService.RemoveProperty("StateId");
                    }
                }
                else {
                    _shoppingCartService.RemoveProperty("CountryId");
                    _shoppingCartService.RemoveProperty("StateId");
                }

            }

            return RedirectToAction("Index");
        }

        public ActionResult Add(int id, int quantity = 1, string itemType = ProductPart.PartItemType, string returnUrl = null) {

            _shoppingCartService.Add(id, itemType, quantity);

            return ReturnOrIndex(returnUrl);
        }

        public ActionResult Remove(int id) {
            _shoppingCartService.Remove(id);

            return RedirectToAction("Index");
        }

        public ActionResult Empty(string returnUrl = null) {
            _shoppingCartService.Empty();

            return ReturnOrIndex(returnUrl);
        }

        private RedirectResult ReturnOrIndex(string returnUrl = null) {
            var urlReferrer = Request.UrlReferrer != null ? Request.UrlReferrer.ToString() : Url.Action("Index");

            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? urlReferrer : returnUrl);
        }

        private void UpdateCart(ShoppingCartItemUpdateViewModel[] CartItems) {
            foreach (var item in CartItems) {
                if (item.IsRemoved) {
                    _shoppingCartService.Remove(item.Id);
                }
                else {
                    _shoppingCartService.UpdateQuantity(item.Id, item.Quantity);
                }
            }
        }

        private Location GetLocation(int CountryId, int StateId) {
            var country = _locationService.GetCountry(CountryId);

            if (country != null && country.Enabled) {
                var state = country.States.Where(s => s.Id == StateId && s.Enabled).FirstOrDefault();
                if (state != null) {
                    return new Location(country, state);
                }
                else {
                    return new Location(country, null);
                }
            }
            else {
                return new Location(null, null);
            }

        }

        private struct Location {
            public Location(LocationsCountryRecord Country, LocationsStateRecord State) {
                this.Country = Country;
                this.State = State;
            }

            public LocationsCountryRecord Country;
            public LocationsStateRecord State;
        }
    }
}