using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
namespace MarketPlaceInfrastructure.Services;
public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        Type enumType = enumValue.GetType();
        string memberName = enumValue.ToString();
        MemberInfo memberInfo = enumType.GetMember(memberName).FirstOrDefault();

        if (memberInfo != null)
        {
            DisplayAttribute displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();

            if (displayAttribute != null)
            {
                return displayAttribute.GetName();
            }
        }

        return memberName;
    }
}