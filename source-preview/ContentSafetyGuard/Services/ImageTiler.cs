using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ContentSafetyGuard.Services
{
    internal class ImageTiler
    {


        public List<Bitmap> CreateGridTilesWithCenter(Bitmap frame)
        {
            List<Bitmap> tiles = new List<Bitmap>();

            int gridSize = 2;
            int tileWidth = frame.Width / gridSize;
            int tileHeight = frame.Height / gridSize;

            // 4 Tiles: oben links, oben rechts, unten links, unten rechts
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int x = col * tileWidth;
                    int y = row * tileHeight;

                    Rectangle sourceRect = new Rectangle(x, y, tileWidth, tileHeight);
                    Bitmap tile = CropBitmap(frame, sourceRect);

                    tiles.Add(tile);
                }
            }

            // 1 Tile in der Mitte
            int centerX = (frame.Width - tileWidth) / 2;
            int centerY = (frame.Height - tileHeight) / 2;

            Rectangle centerRect = new Rectangle(centerX, centerY, tileWidth, tileHeight);
            Bitmap centerTile = CropBitmap(frame, centerRect);

            tiles.Add(centerTile);

            //vollständig
            tiles.Add(frame);

            return tiles;
        }

        private Bitmap CropBitmap(Bitmap source, Rectangle sourceRect)
        {
            Bitmap tile = new Bitmap(sourceRect.Width, sourceRect.Height);

            using (Graphics g = Graphics.FromImage(tile))
            {
                Rectangle destinationRect = new Rectangle(0, 0, sourceRect.Width, sourceRect.Height);
                g.DrawImage(source, destinationRect, sourceRect, GraphicsUnit.Pixel);
            }

            return tile;
        }


    }
}
