using Nfw.Linux.Hid.Joystick;

using System.Diagnostics;

namespace XacAssist.Utility {
    public static class AxisHelper {
         public static sbyte ScaleAxisValue(short inputValue) {
            sbyte result = 0;
            if (inputValue != 0) {
                float percentOfMax = Math.Abs(inputValue) / 32767.0f;
                int newValue = (int) (percentOfMax * (float) (SimpleJoystick.MAX_AXIS_VALUE + 1));
                if (inputValue < 0) newValue *= -1;
                newValue = Math.Clamp(newValue, SimpleJoystick.MIN_AXIS_VALUE + 1, SimpleJoystick.MAX_AXIS_VALUE);
                result = (sbyte) newValue;
            }            
            return result;
        }
    }
}