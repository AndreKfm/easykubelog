using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace EasyKubeLogService.Styles
{
    public interface IButtonStyles
    {
        string ButtonRedDefaultLayout(int pixelWidth = ButtonPixelWidth);
        string ButtonGreenDefaultLayout(int pixelWidth = ButtonPixelWidth);
        const int ButtonPixelWidth = 100;
        string ButtonRed();
        string ButtonGreen();
    }

    public interface IDefaultStyles
    {
        public string DefaultBackgroundControls();
        public string WidthPixel(int pixelWidth);
        public string DefaultControlStyleTopScaled();
        public string DefaultControlsLayoutRemWidthPx(double leftRem = 0.0, int width = 0);
        public string DefaultControlsLayout();
        public string DefaultControlStyle();
        public string TopScaled();
    }

    public interface IUiStyles : IButtonStyles
    {
    }

    public class DefaultStyles : IDefaultStyles
    {
        public string DefaultBackgroundControls() => "background: rgba(200, 200, 200, 0.9);";
        public string WidthPixel(int pixelWidth)
        {
            return $"width:{pixelWidth}px; ";
        }
        public string DefaultControlsLayoutRemWidthPx(double leftRem = 0.0, int width = 0) 
        {
            string leftRemString = $"{leftRem.ToString(CultureInfo.InvariantCulture)}";
            string remWidthString = $"{((leftRem == 0.0 && width == 0) ? String.Empty : $"margin-left: {leftRemString}rem; width:{width}px")}";
            return $"border: 0; align-self: center; {remWidthString}";
        }

        public string DefaultControlsLayout() => DefaultControlsLayoutRemWidthPx(0.0, 0);

        public string DefaultControlStyle() => DefaultBackgroundControls() + DefaultControlsLayout();
        public string TopScaled() => "margin-top: 0.5rem; transform: scale(0.8);";

        public string DefaultControlStyleTopScaled() =>  DefaultControlStyle() + TopScaled();
        
    }

    public class StylesImpl :  IDefaultStyles, IButtonStyles
    {
        private readonly IDefaultStyles _defaultStyles;

        public StylesImpl(IDefaultStyles defaultStyles)
        {
            _defaultStyles = defaultStyles;
        }

        public string ButtonRedDefaultLayout(int pixelWidth = IButtonStyles.ButtonPixelWidth)
        {
            return ButtonRed() + DefaultControlsLayout() + WidthPixel(pixelWidth);
        }

        public string ButtonGreenDefaultLayout(int pixelWidth = IButtonStyles.ButtonPixelWidth)
        {
            return ButtonGreen() + DefaultControlsLayout() + WidthPixel(pixelWidth);
        }

        public string ButtonRed()
        {
            return "color:black; background: rgba(255, 150, 150, 0.9);";
        }

        public string ButtonGreen()
        {
            return "color:black; background: rgba(150, 255, 150, 0.9);";
        }


        public string DefaultBackgroundControls()
        {
            return _defaultStyles.DefaultBackgroundControls();
        }

        public string WidthPixel(int pixelWidth)
        {
            return _defaultStyles.WidthPixel(pixelWidth);
        }

        public string DefaultControlStyleTopScaled()
        {
            return _defaultStyles.DefaultControlStyleTopScaled();
        }

        public string DefaultControlsLayoutRemWidthPx(double leftRem = 0, int width = 0)
        {
            return _defaultStyles.DefaultControlsLayoutRemWidthPx(leftRem, width);
        }

        public string DefaultControlsLayout()
        {
            return _defaultStyles.DefaultControlsLayout();
        }

        public string DefaultControlStyle()
        {
            return _defaultStyles.DefaultControlStyle();
        }

        public string TopScaled()
        {
            return _defaultStyles.TopScaled();
        }
    }
}
