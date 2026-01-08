using System.Collections.Generic;

namespace AshNCircuit.Core.Map
{
    public static class LineOfSightCalculator
    {
        public static List<GridPosition> GetLinearTrajectory(
            int width,
            int height,
            int startX,
            int startY,
            int deltaX,
            int deltaY,
            int maxRange)
        {
            var result = new List<GridPosition>();

            if ((deltaX == 0 && deltaY == 0) || maxRange <= 0)
            {
                return result;
            }

            var currentX = startX;
            var currentY = startY;

            for (var step = 0; step < maxRange; step++)
            {
                currentX += deltaX;
                currentY += deltaY;

                if (!IsInBounds(width, height, currentX, currentY))
                {
                    break;
                }

                result.Add(new GridPosition(currentX, currentY));
            }

            return result;
        }

        public static List<GridPosition> GetLineTrajectory(int width, int height, GridPosition start, GridPosition end, int maxRange)
        {
            var result = new List<GridPosition>();

            if (maxRange <= 0)
            {
                return result;
            }

            var x0 = start.X;
            var y0 = start.Y;
            var x1 = end.X;
            var y1 = end.Y;

            var dx = System.Math.Abs(x1 - x0);
            var dy = System.Math.Abs(y1 - y0);

            if (dx == 0 && dy == 0)
            {
                return result;
            }

            var stepX = x0 < x1 ? 1 : -1;
            var stepY = y0 < y1 ? 1 : -1;

            var err = dx - dy;

            var currentX = x0;
            var currentY = y0;

            var steps = 0;

            while (steps < maxRange)
            {
                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    currentX += stepX;
                }

                if (e2 < dx)
                {
                    err += dx;
                    currentY += stepY;
                }

                if (!IsInBounds(width, height, currentX, currentY))
                {
                    break;
                }

                result.Add(new GridPosition(currentX, currentY));
                steps++;

                if (currentX == x1 && currentY == y1)
                {
                    break;
                }
            }

            return result;
        }

        private static bool IsInBounds(int width, int height, int x, int y)
        {
            return x >= 0 && y >= 0 && x < width && y < height;
        }
    }
}

