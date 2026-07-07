using System.Collections.Generic;

namespace CraftConnectPOS.Data
{
    public interface IRepository<T> where T : EntityBase
    {
        List<T> GetAll();
        T GetById(int id);
        void Add(T entity);
        void Update(T entity);
        void Delete(int id);
        bool Exists(int id);
        int GetCount();
    }
}
