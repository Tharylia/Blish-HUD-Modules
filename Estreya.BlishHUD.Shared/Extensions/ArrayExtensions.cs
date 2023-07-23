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

    public static void MoveEntry<T>(this T[] array, int oldIndex, int newIndex)
    {
        // TODO: Argument validation
        if (oldIndex == newIndex)
        {
            return; // No-op
        }
        T tmp = array[oldIndex];
        if (newIndex < oldIndex)
        {
            // Need to move part of the array "up" to make room
            Array.Copy(array, newIndex, array, newIndex + 1, oldIndex - newIndex);
        }
        else
        {
            // Need to move part of the array "down" to fill the gap
            Array.Copy(array, oldIndex + 1, array, oldIndex, newIndex - oldIndex);
        }
        array[newIndex] = tmp;
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