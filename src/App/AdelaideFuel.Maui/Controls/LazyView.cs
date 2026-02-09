using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace AdelaideFuel.Maui.Controls;

public interface ILazyView : IView
{
    bool AutoLoad { get; set; }
    int AutoLoadDelayMs { get; set; }

    bool StillLazy { get; }
    Type ContentType { get; }

    bool IsVisible { get; set; }
    View Content { get; }

    void LoadView();
}

public abstract class LazyViewBase : ContentView, ILazyView, IDisposable
{
    protected LazyViewBase()
    {
        IsVisible = false;

        void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsVisible) && IsVisible)
            {
                PropertyChanged -= onPropertyChanged;
                if (AutoLoad && StillLazy)
                {
                    if (AutoLoadDelayMs > 0)
                    {
                        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(AutoLoadDelayMs), LoadView);
                    }
                    else
                    {
                        LoadView();
                    }
                }
            }
        }

        PropertyChanged += onPropertyChanged;
    }

    public bool AutoLoad { get; set; } = true;
    public int AutoLoadDelayMs { get; set; } = 0;

    public virtual Type ContentType => Content?.GetType() ?? typeof(View);

    public abstract bool StillLazy { get; }
    public abstract void LoadView();

    public void Dispose()
    {
        if (Content is IDisposable disposable)
            disposable.Dispose();

        GC.SuppressFinalize(this);
    }
}

public class LazyView<TView> : LazyViewBase where TView : View, new()
{
    public LazyView() : base()
    {
    }

    public TView GetView() => Content as TView;

    public override Type ContentType => typeof(TView);

    public override bool StillLazy => Content is not TView;

    public override void LoadView()
    {
        Content ??= new TView();
    }
}

public class LazyView : LazyViewBase
{
    public static readonly BindableProperty TemplateProperty =
        BindableProperty.Create(
            propertyName: nameof(Template),
            returnType: typeof(DataTemplate),
            declaringType: typeof(LazyView),
            defaultValue: null,
            propertyChanged: OnTemplateChanged);

    private readonly Type _viewType;

    public LazyView() : base()
    {
    }

    public LazyView([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type viewType) : base()
    {
        _viewType = viewType;
        Template = new DataTemplate(viewType);
    }

    public DataTemplate Template
    {
        get => (DataTemplate)GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public override Type ContentType => _viewType ?? base.ContentType;

    public override bool StillLazy => Content is null;

    public override void LoadView()
    {
        Content ??= Template?.CreateContent() as View;
    }

    private static void OnTemplateChanged(BindableObject sender, object oldValue, object newValue)
    {
        var lazyView = (LazyView)sender;

        if (newValue is not null && lazyView.AutoLoad && lazyView.StillLazy && lazyView.IsVisible)
            lazyView.LoadView();
    }
}