using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AverageImage
{
    public class ServiceImageUtils
    {
        public static Color GetPopularColour(Bitmap bitMap)
        {
            var coloursInImage = new Dictionary<int, int>();

            // Lock the image Bitmap
            Rectangle rect = new Rectangle(0, 0, bitMap.Width, bitMap.Height);
            BitmapData bmpData = bitMap.LockBits(rect, ImageLockMode.ReadOnly, bitMap.PixelFormat);

            // Point to the first line of image
            IntPtr ptr = bmpData.Scan0;

            // Calculate total pixels (stride is the width of a single row of pixels (a scan line), rounded up to a four-byte boundary)
            int totalPixels = Math.Abs(bmpData.Stride) * bitMap.Height;

            byte[] rgbValues = new byte[totalPixels];

            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, totalPixels);

            // 4 bytes per pixel
            for (int i = 0; i < totalPixels; i += 4)
            {
                byte a = 255;
                byte b = rgbValues[i + 2];
                byte g = rgbValues[i + 1];
                byte r = rgbValues[i];

                var pixelColor = Color.FromArgb(a, b, g, r).ToArgb();

                if (coloursInImage.Keys.Contains(pixelColor))
                {
                    // bump count for this colour
                    coloursInImage[pixelColor]++;
                }
                else
                {
                    // add this colour
                    coloursInImage.Add(pixelColor, 1);
                }

            }

            bitMap.UnlockBits(bmpData);
            return Color.FromArgb(coloursInImage.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key);

        }

        // ***************************************************************************************************
        // Responsible for retreiving a Color object calculated from the passed in most popular colour
        // We are using 'KnownColor's here, this is where we would add our own lookups if this is not suitable
        // ***************************************************************************************************
        private static Color GetColourName(Color inputColour)
        {
            var inputRed = Convert.ToDouble(inputColour.R);
            var inputGreen = Convert.ToDouble(inputColour.G);
            var inputBlue = Convert.ToDouble(inputColour.B);
            var colours = new List<Color>();
            var closestColourName = Color.Empty;
            var distance = 500.0;

            // Build a list of all known, non system colours
            foreach (var knownColor in Enum.GetValues(typeof(KnownColor)))
            {
                var color = Color.FromKnownColor((KnownColor)knownColor);

                if (!color.IsSystemColor)
                {
                    colours.Add(color);
                }
            }

            foreach (var color in colours)
            {
                // Compute Euclidean distance between the two colors
                var testRed = Math.Pow(Convert.ToDouble(color.R) - inputRed, 2.0);
                var testGreen = Math.Pow(Convert.ToDouble(color.G) - inputGreen, 2.0);
                var testBlue = Math.Pow(Convert.ToDouble(color.B) - inputBlue, 2.0);
                var tempDistance = Math.Sqrt(testBlue + testGreen + testRed);

                if (tempDistance == 0.0)
                {
                    // Exact match - return this color
                    return color;
                }

                if (tempDistance < distance)
                {
                    // If this colour is closer to a known colour than the last, then save and update with current details (find the closest)
                    distance = tempDistance;
                    closestColourName = color;
                }
            }

            return closestColourName;
        }

        // ******************************************************************************************************************
        // Download the supplied image file (url) as a byte array, convert to a memory stream and then finaally into a Bitmap
        // Calaculate the most used colour in this bitmap (GetPopularColour)
        // Convert this most used colour into a known colour name (GetColourName)
        // ******************************************************************************************************************
        public async Task<string> DownloadAndProcessImage(string url)
        {
            byte[] imageBytes;

            try
            {
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(System.Net.WebUtility.UrlDecode(url)))
                    {
                        imageBytes = await response.Content.ReadAsByteArrayAsync();
                        var ms = new MemoryStream(imageBytes);
                        Bitmap image = (Bitmap)Bitmap.FromStream(ms);

                        var mostUsedColor = GetPopularColour(image);
                        var color = GetColourName(mostUsedColor);

                        return color.ToString();
                    }
                }

            }
            catch (Exception ex)
            {
                // this could be expanded more to (probably) return a single error code but for demo, we simply display a string
                return "ERROR OCCURED WITH THE FOLLOWING FILE: " + ex.Message;
            }
        }
    }
}
