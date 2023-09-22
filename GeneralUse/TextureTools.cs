using System;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public class TextureTools
    {
        public static Texture2D LoadFromPng(string filePath)
        {
            if (!System.IO.File.Exists(filePath)) return null;

            var fileData = System.IO.File.ReadAllBytes(filePath);
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData)) //..this will auto-resize the texture dimensions.
            {
                return texture;
            }
            return null;
        }
        
        public static void SaveAsPng(Texture2D texture, string filePath)
        {
            if (texture == null) throw new ArgumentException("Tried to save null texture");

            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(filePath, bytes);
            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            #endif
        }
    }
}