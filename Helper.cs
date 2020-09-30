using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Schema;

namespace basic_script_interpreter
{
    public static class Helper
    {

        public static bool IsNumericInt(object value)
        {
            int result = 0;
            if (int.TryParse(value.ToString(), out result))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsNumericDouble(object value)
        {
            double result = 0;
            if (double.TryParse(value.ToString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
