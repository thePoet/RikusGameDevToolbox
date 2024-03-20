namespace RikusGameDevToolbox.GeneralUse
{
    public static class NumberTypeExtensionMethods
    {
        public static float MultiplyIf(this float number, bool condition, float multiplier) 
        {
            if (condition) return number * multiplier;
            return number;
        }
    }
}