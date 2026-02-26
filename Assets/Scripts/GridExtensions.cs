using System.Collections.Generic;

public static class GridExtensions
{
    public static void GetNeighbors( this List<int>[,] grid, int gridX, int gridY, int range, List<int> result )
    {
        result.Clear();

        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for (int y = gridY - range; y <= gridY + range; y++)
        {
            if (y < 0 || y >= height)
            {
                continue;
            }

            for (int x = gridX - range; x <= gridX + range; x++)
            {
                if (x < 0 || x >= width)
                {
                    continue;
                }

                List<int> cell = grid[x, y];

                for (int i = 0; i < cell.Count; i++)
                {
                    result.Add(cell[i]);
                }
            }
        }
    }
}
