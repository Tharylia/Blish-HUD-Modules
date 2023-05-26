namespace Estreya.BlishHUD.Shared.Extensions;

using System;

public static class ArrayExtensions
{
    public static void ForEach(this Array array, Action<Array, int[]> action)
    {
        if (array.LongLength == 0)
        {
            return;
        }

        ArrayTraverse walker = new ArrayTraverse(array);
        do
        {
            action(array, walker.Position);
        } while (walker.Step());
    }
}

internal class ArrayTraverse
{
    private readonly int[] maxLengths;
    public int[] Position;

    public ArrayTraverse(Array array)
    {
        this.maxLengths = new int[array.Rank];
        for (int i = 0; i < array.Rank; ++i)
        {
            this.maxLengths[i] = array.GetLength(i) - 1;
        }

        this.Position = new int[array.Rank];
    }

    public bool Step()
    {
        for (int i = 0; i < this.Position.Length; ++i)
        {
            if (this.Position[i] < this.maxLengths[i])
            {
                this.Position[i]++;
                for (int j = 0; j < i; j++)
                {
                    this.Position[j] = 0;
                }

                return true;
            }
        }

        return false;
    }
}