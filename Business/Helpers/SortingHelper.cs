using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace Business.Helpers
{
    public static class SortingHelper
    {
        // Very small, allocation-free, and marked for aggressive inlining.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSortDescending(ReadOnlySpan<char> sortDir)
        {
            if (sortDir.Length == 0) return true; // default to descending

            // Trim leading/trailing whitespace without allocations
            int i = 0, j = sortDir.Length - 1;
            while (i <= j && char.IsWhiteSpace(sortDir[i])) i++;
            while (j >= i && char.IsWhiteSpace(sortDir[j])) j--;
            var tokenLen = j - i + 1;
            if (tokenLen != 4) return false; // only "desc" considered descending

            // Compare characters case-insensitively without allocations
            return (char.ToLowerInvariant(sortDir[i]) == 'd'
                && char.ToLowerInvariant(sortDir[i + 1]) == 'e'
                && char.ToLowerInvariant(sortDir[i + 2]) == 's'
                && char.ToLowerInvariant(sortDir[i + 3]) == 'c');
        }

        // Convenience overload for string callers
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static bool IsSortDescending(string? sortDir)
            => IsSortDescending(sortDir is null ? ReadOnlySpan<char>.Empty : sortDir.AsSpan());
    }
}