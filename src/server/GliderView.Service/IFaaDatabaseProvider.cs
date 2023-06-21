namespace GliderView.Service
{
    /// <summary>
    /// This class provides a link to the FAA aircraft registration database. It uses a screen scraper
    /// to get aircraft information based on N-number.
    /// </summary>
    public interface IFaaDatabaseProvider
    {
        Task<FaaDatabaseProvider.Aircraft?> Lookup(string nNumber);
    }
}