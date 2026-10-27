namespace VSOP87
{
    /// <summary>
    /// Selects the vector width used by <see cref="Calculator"/> and <see cref="CalculatorF"/> to evaluate the series.
    /// </summary>
    public enum SimdMode
    {
        /// <summary>Pick the widest hardware-accelerated width available.</summary>
        Auto = 0,

        /// <summary>Scalar evaluation only.</summary>
        Scalar = 1,

        /// <summary>128-bit vectors (2 doubles / 4 floats).</summary>
        Vector128 = 2,

        /// <summary>256-bit vectors (4 doubles / 8 floats).</summary>
        Vector256 = 3,

        /// <summary>512-bit vectors (8 doubles / 16 floats).</summary>
        Vector512 = 4,
    }
}
