using System.ComponentModel;
using Color = Microsoft.Maui.Graphics.Color;
using ImageButton = Microsoft.Maui.Controls.ImageButton;
using MauiView = Microsoft.Maui.Controls.View;

#if ANDROID
using Android.Content.Res;
using Android.Graphics;
using Android.Widget;
using AndroidX.Core.Graphics.Drawable;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AndroidMaterialButton = Google.Android.Material.Button.MaterialButton;
using AndroidWidgetButton = Android.Widget.Button;
#endif

#if IOS || MACCATALYST
using UIKit;
using Microsoft.Maui.Platform;
#endif

namespace AdelaideFuel.Maui;

/// <summary>
/// A behavior that allows tinting icons/images across platforms.
/// </summary>
public sealed class IconTintColorBehavior : Behavior<MauiView>
{
	#region BindableProperty

	public static readonly BindableProperty TintColorProperty =
		BindableProperty.Create(
			nameof(TintColor),
			typeof(Color),
			typeof(IconTintColorBehavior),
			null,
			propertyChanged: OnTintColorChanged);

	public Color TintColor
	{
		get => (Color)GetValue(TintColorProperty);
		set => SetValue(TintColorProperty, value);
	}

	private static void OnTintColorChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((IconTintColorBehavior)bindable).ApplyTint();
	}

	#endregion

	private MauiView _associatedView;

	protected override void OnAttachedTo(MauiView bindable)
	{
		base.OnAttachedTo(bindable);

		_associatedView = bindable;
		bindable.PropertyChanged += OnViewPropertyChanged;
		bindable.HandlerChanged += OnHandlerChanged;

		ApplyTint();
	}

	protected override void OnDetachingFrom(MauiView bindable)
	{
		bindable.HandlerChanged -= OnHandlerChanged;
		bindable.PropertyChanged -= OnViewPropertyChanged;

		ClearTint();
		_associatedView = null;

		base.OnDetachingFrom(bindable);
	}

	private void OnHandlerChanged(object sender, EventArgs e)
	{
		ApplyTint();
	}

	private void OnViewPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != Image.SourceProperty.PropertyName &&
			e.PropertyName != ImageButton.SourceProperty.PropertyName &&
			e.PropertyName != ImageButton.IsLoadingProperty.PropertyName)
		{
			return;
		}

		ApplyTint();
	}

	private void ApplyTint()
	{
		if (_associatedView?.Handler?.PlatformView is null)
			return;

		if (TintColor is null)
		{
			ClearTint();
			return;
		}

#if ANDROID
		ApplyAndroidTint(_associatedView.Handler.PlatformView as AView, TintColor);
#endif

#if IOS || MACCATALYST
		ApplyAppleTint(_associatedView.Handler.PlatformView as UIView, TintColor);
#endif
	}

	private void ClearTint()
	{
		if (_associatedView?.Handler?.PlatformView is null)
			return;

#if ANDROID
		ClearAndroidTint(_associatedView.Handler.PlatformView as AView);
#endif

#if IOS || MACCATALYST
		ClearAppleTint(_associatedView.Handler.PlatformView as UIView);
#endif
	}

#if ANDROID

	private static void ApplyAndroidTint(AView nativeView, Color tintColor)
	{
		if (nativeView is null)
			return;

		try
		{
			var platformColor = tintColor.ToPlatform();

			switch (nativeView)
			{
				case ImageView image:
					image.SetColorFilter(
						new PorterDuffColorFilter(platformColor, PorterDuff.Mode.SrcIn));
					break;

				case AndroidMaterialButton materialButton:
					materialButton.IconTintMode = PorterDuff.Mode.SrcIn;
					materialButton.IconTint = ColorStateList.ValueOf(platformColor);
					break;

				case AndroidWidgetButton widgetButton:
					foreach (var drawable in widgetButton.GetCompoundDrawables())
					{
						if (drawable is null)
							continue;

						DrawableCompat.SetTint(drawable, platformColor);
					}
					break;
			}
		}
		catch (ObjectDisposedException)
		{
			// Handler/view disposed during lifecycle transition – safe to ignore.
		}
	}

	private static void ClearAndroidTint(AView nativeView)
	{
		if (nativeView is null)
			return;

		try
		{
			switch (nativeView)
			{
				case ImageView image:
					image.ClearColorFilter();
					break;

				case AndroidMaterialButton materialButton:
					materialButton.IconTint = null;
					break;

				case AndroidWidgetButton widgetButton:
					foreach (var drawable in widgetButton.GetCompoundDrawables())
					{
						drawable?.ClearColorFilter();
					}
					break;
			}
		}
		catch (ObjectDisposedException)
		{
			// Expected during teardown.
		}
	}

#endif

#if IOS || MACCATALYST

	private static void ApplyAppleTint(UIView platformView, Color tintColor)
	{
		if (platformView is null)
			return;

		var uiColor = tintColor.ToPlatform();

		switch (platformView)
		{
			case UIImageView imageView when imageView.Image is not null:
				imageView.Image =
					imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
				imageView.TintColor = uiColor;
				break;

			case UIButton button when button.ImageView?.Image is not null:
				button.ImageView.Image =
					button.ImageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
				button.TintColor = uiColor;
				break;
		}
	}

	private static void ClearAppleTint(UIView platformView)
	{
		if (platformView is null)
			return;

		switch (platformView)
		{
			case UIImageView imageView when imageView.Image is not null:
				imageView.Image =
					imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
				break;

			case UIButton button when button.ImageView?.Image is not null:
				button.ImageView.Image =
					button.ImageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
				break;
		}
	}

#endif
}