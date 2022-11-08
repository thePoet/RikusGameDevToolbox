

// Collection of very broad use C# functions

namespace RikusGameDevToolbox.GeneralUse
{
    public static class Generic
    {
        // Returns the number of enumerators/names in an enum type.
        // Use: int num = NumEnumerators<MyEnumType>();
        public static int NumEnumerators<T>()
        {
            return System.Enum.GetNames(typeof(T)).Length;
        }

        // Returns name as string for an enumerator with given index in an enum type.
        public static string EnumeratorName<T>(int index)
        {
            if (index >= NumEnumerators<T>())
                return null;
            return System.Enum.GetNames(typeof(T))[index];
        }

    }

}
