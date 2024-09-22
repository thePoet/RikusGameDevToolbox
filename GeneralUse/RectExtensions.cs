using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class RectExtensions
    {
        public static Rect Grow(this Rect r, float amount)
        {
            Vector2 position = r.position - new Vector2(amount * 0.5f, amount * 0.5f);
            Vector2 size = r.size + new Vector2(amount, amount);
            return new Rect(position, size);
        }
        
        public static Rect Shrink(this Rect r, float amount) => Grow(r, -amount);
    }
}