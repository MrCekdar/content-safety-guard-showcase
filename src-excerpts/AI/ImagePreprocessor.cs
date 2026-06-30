using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ContentSafetyGuard.AI
{
    internal class ImagePreprocessor
    {
        public DenseTensor<float> BitmapToTensor(Bitmap image) // Für die KI vorbereiten
        {
            DenseTensor<float> imageTensor = new DenseTensor<float>(new[] { 1, 3, image.Height, image.Width });

            for (int i = 0; i < image.Height; i++)
            {
                for (int j = 0; j < image.Width; j++)
                {
                    System.Drawing.Color pixel = image.GetPixel(j, i);
                    float r = (pixel.R / 255f - 0.5f) / 0.5f;
                    float g = (pixel.G / 255f - 0.5f) / 0.5f;
                    float b = (pixel.B / 255f - 0.5f) / 0.5f;

                    imageTensor[0, 0, i, j] = r;
                    imageTensor[0, 1, i, j] = g;
                    imageTensor[0, 2, i, j] = b;
                }
            }

            return imageTensor;
        }

        public Bitmap ResizeCapturedFrame(Bitmap frame, int width, int height) // Funktion um das Screenshot zu verkleinern
        {
            Bitmap resizedImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resizedImage))
            {
                g.DrawImage(frame, 0, 0, width, height);
            }
            return resizedImage;
        }
   
    }


}
