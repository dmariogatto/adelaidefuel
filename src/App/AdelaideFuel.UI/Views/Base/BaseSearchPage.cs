﻿using AdelaideFuel.ViewModels;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    public partial class BaseSearchPage<T> : BasePage<T>, ISearchPage where T : BaseViewModel
    {
        public static readonly BindableProperty QueryProperty =
            BindableProperty.CreateAttached(
                propertyName: nameof(Query),
                returnType: typeof(string),
                declaringType: typeof(BaseSearchPage<>),
                defaultValue: string.Empty);

        public static readonly BindableProperty PlaceholderProperty =
            BindableProperty.CreateAttached(
                propertyName: nameof(Placeholder),
                returnType: typeof(string),
                declaringType: typeof(BaseSearchPage<>),
                defaultValue: Localisation.Resources.Search);

        public BaseSearchPage() : base()
        {
        }

        public string Query
        {
            get => (string)GetValue(QueryProperty);
            set => SetValue(QueryProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }
    }
}