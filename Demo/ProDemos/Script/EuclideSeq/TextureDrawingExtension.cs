using System;
using UnityEngine;

namespace MPTKDemoEuclidean
{
    public static class TextureDrawingExtension
    {
        static Color ColorFill = Color.white;
        static Color ColorBorder = Color.black;
        public static void SetColorFill(this Texture2D tex, Color color)
        {
            ColorFill = color;
        }

        public static void SetColorBorder(this Texture2D tex, Color color)
        {
            ColorBorder = color;
        }

        public static void Clear(this Texture2D tex)
        {
            Color[] pixel = tex.GetPixels();
            Array.Clear(pixel, 0, pixel.Length);
            tex.SetPixels(pixel);
        }

        public static void DrawCircle(this Texture2D tex, int x, int y, bool fill, int radius = 3, int border = 0)
        {
            float rSquared = radius * radius;
            float rSquaredBorder = (radius - border) * (radius - border);

            for (int u = x - radius; u < x + radius + 1; u++)
                for (int v = y - radius; v < y + radius + 1; v++)
                {
                    int pos = (x - u) * (x - u) + (y - v) * (y - v);
                    if (pos < rSquared)
                        if (pos > rSquaredBorder && border > 0)
                            tex.SetPixel(u, v, ColorBorder);
                        else if (fill)
                            tex.SetPixel(u, v, ColorFill);
                }
        }

        public static void DrawRectangle(this Texture2D tex, Color colorFill, Color colorBorder, int width, int height, int border = 0)
        {
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    if (i < border || i > width - border || j < border || j > height - border)
                        tex.SetPixel(i, j, colorBorder);
                    else
                        tex.SetPixel(i, j, colorFill);
        }

        /// <summary>@brief
        /// Get the rectangle in screen coordinates for this RectTransform
        /// </summary>
        /// <returns>Rect in screen coordinate.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera.</param>
        public static Rect GetScreenRect(this RectTransform rectTransform, Camera camera)
        {
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);
            for (var i = 0; i < objectCorners.Length; i++) // For each corner in rectTransform
                objectCorners[i] = camera.WorldToScreenPoint(objectCorners[i]); // Transform world space position of corner to screen space

            Vector2 size = new Vector2(objectCorners[3].x - objectCorners[0].x, objectCorners[1].y - objectCorners[0].y);
            return new Rect(objectCorners[0], size);
        }

        /// <summary>@brief
        /// Counts the bounding box corners of the given RectTransform that are visible in screen space.
        /// See here https://forum.unity.com/threads/test-if-ui-element-is-visible-on-screen.276549/
        /// </summary>
        /// <returns>The amount of bounding box corners that are visible.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
        public static bool CountCornersVisibleFromRect(this RectTransform rectTransform, Vector3 fromTransform, Camera camera = null)
        {
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            Vector3 inside = camera.WorldToScreenPoint(fromTransform);

            Vector3[] objectCornersScreen = new Vector3[4];

            for (var i = 0; i < objectCorners.Length; i++) // For each corner in rectTransform
            {
                objectCornersScreen[i] = camera.WorldToScreenPoint(objectCorners[i]); // Transform world space position of corner to screen space
            }

            Vector2 size = new Vector2(objectCornersScreen[3].x - objectCornersScreen[0].x, objectCornersScreen[1].y - objectCornersScreen[0].y);
            Rect rect = new Rect(objectCornersScreen[0], size);
            return rect.Contains(inside);
        }
        /// <summary>@brief
        /// Counts the bounding box corners of the given RectTransform that are visible in screen space.
        /// See here https://forum.unity.com/threads/test-if-ui-element-is-visible-on-screen.276549/
        /// </summary>
        /// <returns>The amount of bounding box corners that are visible.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
        public static int CountCornersVisibleFrom(this RectTransform rectTransform, Camera camera = null)
        {
            Rect screenBounds = new Rect(0f, 0f, Screen.width, Screen.height); // Screen space bounds (assumes camera renders across the entire screen)
            Vector3[] objectCorners = new Vector3[4];
            rectTransform.GetWorldCorners(objectCorners);

            int visibleCorners = 0;
            Vector3 tempScreenSpaceCorner; // Cached
            for (var i = 0; i < objectCorners.Length; i++) // For each corner in rectTransform
            {
                if (camera != null)
                    tempScreenSpaceCorner = camera.WorldToScreenPoint(objectCorners[i]); // Transform world space position of corner to screen space
                else
                {
                    //Debug.Log(rectTransform.gameObject.name + " : " + objectCorners[i].ToString("F2"));
                    tempScreenSpaceCorner = objectCorners[i]; // If no camera is provided we assume the canvas is Overlay and world space == screen space
                }

                if (screenBounds.Contains(tempScreenSpaceCorner)) // If the corner is inside the screen
                {
                    visibleCorners++;
                }
            }
            return visibleCorners;
        }
        /// <summary>@brief
        /// Determines if this RectTransform is fully visible.
        /// Works by checking if each bounding box corner of this RectTransform is inside the screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is fully visible; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
        public static bool IsFullyVisibleFrom(this RectTransform rectTransform, Camera camera = null)
        {
            if (!rectTransform.gameObject.activeInHierarchy)
                return false;

            return CountCornersVisibleFrom(rectTransform, camera) == 4; // True if all 4 corners are visible
        }

        /// <summary>@brief
        /// Determines if this RectTransform is at least partially visible.
        /// Works by checking if any bounding box corner of this RectTransform is inside the screen space view frustrum.
        /// </summary>
        /// <returns><c>true</c> if is at least partially visible; otherwise, <c>false</c>.</returns>
        /// <param name="rectTransform">Rect transform.</param>
        /// <param name="camera">Camera. Leave it null for Overlay Canvasses.</param>
        public static bool IsVisibleFrom(this RectTransform rectTransform, Camera camera = null)
        {
            if (!rectTransform.gameObject.activeInHierarchy)
                return false;

            return CountCornersVisibleFrom(rectTransform, camera) > 0; // True if any corners are visible
        }
    }
}
