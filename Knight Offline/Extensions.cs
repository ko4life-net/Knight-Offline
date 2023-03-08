using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Knight_Offline
{
    public static class EnumExtensionMethods
    {
        // This method is not idiot proof, vide ArgumentNullException if you screw up
        public static string GetDescription(this Enum Value)
        {
            return Value.GetType().GetField(Value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute)).Cast<DescriptionAttribute>().Select(x => x.Description).FirstOrDefault().ToString();

            // DescriptionAttribute[] DescriptionAttributes = Value.GetType().GetField(Value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute)) as DescriptionAttribute[];

            // return DescriptionAttributes.Select(x => x.Description).FirstOrDefault().ToString();
        }
    }
}