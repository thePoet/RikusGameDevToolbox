namespace RikusGameDevToolbox.GeneralUse
{
    public static class ArrayExtensionMethods
    {
        
        /// <summary>
        /// Sets all the 
        /// </summary>
        public static void Populate<T>(this T[] arr, T value ) 
        {
            for ( int i = 0; i < arr.Length;i++ ) 
            {
                arr[i] = value;
            }
        }
    }
}