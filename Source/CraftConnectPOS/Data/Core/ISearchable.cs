using System.Collections.Generic;

namespace CraftConnectPOS.Data
{
    public interface ISearchable<T>
    {
        List<T> Search(string searchTerm);
    }
}
