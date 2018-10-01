using System;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XamEffects;
using XamEffects.Droid;
using XamEffects.Droid.Collectors;
using Color = Android.Graphics.Color;
using View = Android.Views.View;

[assembly: ResolutionGroupName("XamEffects")]
[assembly: ExportEffect(typeof(TouchEffectPlatform), nameof(TouchEffect))]
namespace XamEffects.Droid
{
	public class TouchEffectPlatform : PlatformEffect
    {
		static TouchEffectPlatform()
		{
			if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
				throw new NotSupportedException($"API level {Build.VERSION.SdkInt} not supported by tweaked XamEffects library. Must be at least {BuildVersionCodes.Lollipop}.");
		}

        private View _view;
        private RippleDrawable _ripple;
        private FrameLayout _viewOverlay;

		public static void Init() { } // no-op, so far

        protected override void OnAttached()
        {
            _view = Control ?? Container;

			if (Control is Android.Widget.ListView || Control is Android.Widget.ScrollView)
            {
                // Except ListView and ScrollView because of Raising Exception OnClick
                return;
            }

	        _view.Clickable = true;
	        _view.LongClickable = true;

			_viewOverlay = new FrameLayout(Container.Context)
	        {
		        LayoutParameters = new ViewGroup.LayoutParams(-1, -1),
                Clickable = false,
                Focusable = false,
	        };
	        Container.LayoutChange += ViewOnLayoutChange;

            AddRipple();
			_view.Touch += OnTouch;

            UpdateEffectColor();
        }
        
        protected override void OnDetached()
        {
			_view.Touch -= OnTouch;
			RemoveRipple();
			ViewOverlayCollector.TryDelete(Container, this);
		}

        private void OnTouch(object sender, View.TouchEventArgs args)
        {
            switch (args.Event.Action)
            {
                case MotionEventActions.Down:
		            ForceStartRipple(args.Event.GetX(), args.Event.GetY());
					break;
                case MotionEventActions.Up:
				case MotionEventActions.Cancel:
                    args.Handled = false;
					ForceEndRipple();
					break;
            }
        }

        protected override void OnElementPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(e);

            if (e.PropertyName == TouchEffect.ColorProperty.PropertyName)
                UpdateEffectColor();
        }

		private Color GetAndroidColor()
		{
			var color = TouchEffect.GetColor(Element);
			var androidColor = color.ToAndroid();
			androidColor.A = 80; // perhaps a bit arbitrary
			return androidColor;
		}

		private void UpdateEffectColor() => _ripple.SetColor(GetPressedColorSelector(GetAndroidColor()));

		private void AddRipple()
        {
			_ripple = CreateRipple(GetAndroidColor());
			_viewOverlay.Background = _ripple;
			Container.AddView(_viewOverlay);
		}

		private void RemoveRipple()
        {
	        _viewOverlay.Foreground = null;
            _ripple?.Dispose();
            _ripple = null;
        }

        private RippleDrawable CreateRipple(Color color)
        {
            if (Element is Layout)
            {
                var mask = new ColorDrawable(Color.White);
                return _ripple = new RippleDrawable(GetPressedColorSelector(color), null, mask);
            }

            var back = _view.Background;
            if (back == null)
            {
                var mask = new ColorDrawable(Color.White);
                return _ripple = new RippleDrawable(GetPressedColorSelector(color), null, mask);
            }
            
			if (back is RippleDrawable)
            {
                _ripple = (RippleDrawable) back.GetConstantState().NewDrawable();
                _ripple.SetColor(GetPressedColorSelector(color));

                return _ripple;
            }

			return _ripple = new RippleDrawable(GetPressedColorSelector(color), back, null);
        }

        private static ColorStateList GetPressedColorSelector(int pressedColor)
        {
            return new ColorStateList(
                new[]
                {
                    new int[]{}
                },
                new[]
                {
                    pressedColor,
                });
        }

		private void ForceStartRipple(float x, float y)
	    {
		    if (_viewOverlay.Background is RippleDrawable bc)
		    {
			    _viewOverlay.BringToFront();
				bc.SetHotspot(x, y);
				_viewOverlay.Pressed = true;
		    }
		}

		private void ForceEndRipple() => _viewOverlay.Pressed = false;

		private void ViewOnLayoutChange(object sender, View.LayoutChangeEventArgs layoutChangeEventArgs)
	    {
		    var group = (ViewGroup)sender;
			_viewOverlay.Right = group.Width;
		    _viewOverlay.Bottom = group.Height;
	    }
	}
}
