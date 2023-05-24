using AdelaideFuel.UI.Services;
using Foundation;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;


namespace AdelaideFuel.iOS.Services
{
    [Preserve(AllMembers = true)]
    public class RendererService_iOS : IRendererService
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