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
    public class utils
    {
        unsafe public static Color GetMostUsedColour(Bitmap bitMap, int mode)
        {
            var colorIncidence = new Dictionary<int, int>();

            if (mode == 1)
            {
                Rectangle rect = new Rectangle(0, 0, bitMap.Width, bitMap.Height);
                BitmapData bmpData = bitMap.LockBits(rect, ImageLockMode.ReadOnly, bitMap.PixelFormat);

                // Get the address of the first line.
                IntPtr ptr = bmpData.Scan0;

                // Declare an array to hold the bytes of the bitmap.
                int totalPixels = Math.Abs(bmpData.Stride) * bitMap.Height;
                byte[] rgbValues = new byte[totalPixels];

                // Copy the RGB values into the array.
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, totalPixels);


                for (long l = 0; l < rgbValues.Length; l++)
                {
                    byte b = rgbValues[l];

                    if (colorIncidence.Keys.Contains(b))
                    {
                        // bump count for this colour
                        colorIncidence[b]++;
                    }
                    else
                    {
                        // add this colour
                        colorIncidence.Add(b, 1);
                    }
                }

                bitMap.UnlockBits(bmpData);
                return Color.FromArgb(colorIncidence.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key);
            }

            if (mode == 2)
            {
                // loop all columns
                for (var x = 0; x < bitMap.Size.Width; x++)
                {
                    // and all rows of image data
                    for (var y = 0; y < bitMap.Size.Height; y++)
                    {
                        var pixelColor = bitMap.GetPixel(x, y).ToArgb();

                        if (colorIncidence.Keys.Contains(pixelColor))
                        {
                            // bump count for this colour
                            colorIncidence[pixelColor]++;
                        }
                        else
                        {
                            // add this colour
                            colorIncidence.Add(pixelColor, 1);
                        }
                    }
                }

                return Color.FromArgb(colorIncidence.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key);
            }

            if(mode == 3)
            {
                Rectangle rect = new Rectangle(0, 0, bitMap.Width, bitMap.Height);
                BitmapData bmpData = bitMap.LockBits(rect, ImageLockMode.ReadOnly, bitMap.PixelFormat);

                IntPtr ptr = bmpData.Scan0;

                int totalPixels = Math.Abs(bmpData.Stride) * bitMap.Height;
                int[] pixelData = new int[totalPixels];
                byte[] rgbValues = new byte[totalPixels];

                for (int i = 0; i < totalPixels; i+=4)
                {
                    byte* pixel = (byte*)ptr;

                    byte a = 255; // pixel[0]; // You can ignore if you do not need alpha.
                    byte b = pixel[3];
                    byte g = pixel[2];
                    byte r = pixel[1];

                    Color c = Color.FromArgb(a, g, r, b);
                    var pixelColor = c.ToArgb();

                    if (colorIncidence.Keys.Contains(pixelColor))
                    {
                        // bump count for this colour
                        colorIncidence[pixelColor]++;
                    }
                    else
                    {
                        // add this colour
                        colorIncidence.Add(pixelColor, 1);
                    }

                }

                return Color.FromArgb(colorIncidence.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key);

            }

            return new Color();
        }

        private static Color GetColourName(Color inputColour)
        {
            var inputRed = Convert.ToDouble(inputColour.R);
            var inputGreen = Convert.ToDouble(inputColour.G);
            var inputBlue = Convert.ToDouble(inputColour.B);
            var colours = new List<Color>();

            // Build a list of all known, non system colours
            foreach (var knownColor in Enum.GetValues(typeof(KnownColor)))
            {
                var color = Color.FromKnownColor((KnownColor)knownColor);

                if (!color.IsSystemColor)
                {
                    colours.Add(color);
                }
            }

            var nearestColor = Color.Empty;
            var distance = 500.0;
            
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
                    nearestColor = color;
                }
            }

            return nearestColor;
        }

        // ******************************************************************************************************************
        // Download the supplied image file (url) as a byte array, convert to a memory stream and then finaally into a Bitmap
        // Calaculate the most used colour in this bitmap (GetMostUsedColour)
        // Convert this most used colour into a known colour name (GetColourName)
        // ******************************************************************************************************************
        public async Task<string> Process(string url, int mode = 1)
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

                        var mostUsedColor = GetMostUsedColour(image, mode);
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
