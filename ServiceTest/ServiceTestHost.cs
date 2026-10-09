using System.Reflection;
using OSDC.Drilling.WellBore.Service.Managers;

namespace OSDC.Drilling.WellBore.ServiceTest;

internal static class ServiceTestHost
{
    internal static void ResetManagerSingletons()
    {
        foreach (Type type in typeof(WellBoreManager).Assembly.GetTypes()
                     .Where(type => type.Namespace == typeof(WellBoreManager).Namespace))
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(field => !field.IsInitOnly && !field.IsLiteral && field.FieldType == type))
            {
                field.SetValue(null, null);
            }
        }
    }
}
