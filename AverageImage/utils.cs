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
        public static Color GetPopularColour(Bitmap bitMap, int mode)
        {
            var coloursInImage = new Dictionary<int, int>();

            // **********************************************************************************************************************************
            // First attempt.  Simply loop through all the columns and rows and use GetPixel to grab the details for each Pixel.
            // Get its Argb details and add to a dictionary of colours present in the image.  Return the most popular one at the end of the loop.
            //
            // Works but is very slow - taking about 30 seconds to calculate for one of the supplied demo images.
            // **********************************************************************************************************************************
            if (mode == 1)
            {
                // loop all columns
                for (var x = 0; x < bitMap.Size.Width; x++)
                {
                    // and all rows of image data
                    for (var y = 0; y < bitMap.Size.Height; y++)
                    {
                        var pixelColor = bitMap.GetPixel(x, y).ToArgb();

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
                }

                return Color.FromArgb(coloursInImage.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key);
            }

            // *************************************************************************************************************************************
            // Second attempt.  A bit of research showed that the GetPixel call is very slow.  A little bit more looking around presented an example
            // using 'LockBits'  I converted the sample code and got it working to a degree.  There was a problem with the RGB values being returned
            // which resulted in incorrect colours being detected as the most common - I think because I was only ever loking at a single colour
            // element from the pixel, rather than all three (RGB).
            //
            // Almost works but much, much quicker.  Takes about 5 seconds to calculate for one of the demo images supplied
            // *************************************************************************************************************************************
            else if (mode == 2)
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

                    if (coloursInImage.Keys.Contains(b))
                    {
                        // bump count for this colour
                        coloursInImage[b]++;
                    }
                    else
                    {
                        // add this colour
                        coloursInImage.Add(b, 1);
                    }
                }

                bitMap.UnlockBits(bmpData);
                return Color.FromArgb(coloursInImage.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value).First().Key);
            }

            //**********************************************************************************************************************
            // Third attempt.  I rewrote version 2 so that it grabbed 3 bytes of pixel data (RGB) and created a Color from them all.
            //
            // Correctly identifies all four demo images supplied within 4 - 5 second per image
            //**********************************************************************************************************************
            else if (mode == 3)
            {
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
                for (int i = 0; i < totalPixels; i+=4)
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

            // Default - in case we dont have a mode passed in
            return new Color();
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
        public async static Task<string> Process(string url, int mode = 1)
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

                        var mostUsedColor = GetPopularColour(image, mode);
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
