namespace GliderView.Service
{
    public interface IFaaDatabaseProvider
    {
        Task<FaaDatabaseProvider.Aircraft?> Lookup(string nNumber);
    }
}