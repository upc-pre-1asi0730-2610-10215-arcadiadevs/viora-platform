namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// Direction of NDVI trend change.
    /// </summary>
    public enum NdviTrendDirection
    {
        /// <summary>
        /// NDVI is increasing (change rate > 0.02).
        /// </summary>
        Rising,

        /// <summary>
        /// NDVI is decreasing (change rate < -0.02).
        /// </summary>
        Falling,

        /// <summary>
        /// NDVI is stable (change rate between -0.02 and 0.02).
        /// </summary>
        Stable
    }
}