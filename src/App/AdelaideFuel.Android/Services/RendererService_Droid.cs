using AdelaideFuel.UI.Services;
using Android.Runtime;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace AdelaideFuel.Droid.Services
{
    [Preserve(AllMembers = true)]
    public class RendererService_Droid : IRendererService
    {
        public object GetRenderer(VisualElement element)
        {
            return Platform.GetRenderer(element);
        }

        public bool HasRenderer(VisualElement element)
        {
            return GetRenderer(element) is not null;
        }

        public void OnRendererSet(VisualElement element, Action<VisualElement, object> callback)
        {
            if (element is not null && callback is not null)
            {
                var renderer = GetRenderer(element);
                if (renderer is not null)
                {
                    callback.Invoke(element, renderer);
                }
                else
                {
                    void elementPropertyChanged(object sender, PropertyChangedEventArgs e)
                    {
                        if (e.PropertyName == "Renderer" &&
                            GetRenderer(element) is IVisualElementRenderer newRenderer)
                        {
                            element.PropertyChanged -= elementPropertyChanged;
                            callback.Invoke(element, newRenderer);
                        }
                    }

                    element.PropertyChanged += elementPropertyChanged;
                }
            }
        }
    }
}