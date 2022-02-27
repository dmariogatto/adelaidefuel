﻿using AdelaideFuel.Models;
using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.SiteSearch)]
    public partial class SiteSearchPage : BaseSearchPage<SiteSearchViewModel>, ISearchPage
    {
        public SiteSearchPage() : base()
        {
            InitializeComponent();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            ViewModel.SearchText = null;
        }

        private void ListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is SiteFuelPrice model)
            {
                ViewModel.TappedCommand.ExecuteAsync(model);
            }
        }
    }
}