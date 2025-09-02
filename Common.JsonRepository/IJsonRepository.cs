using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.JsonRepository
{
    public interface IJsonRepository<T>
    {
        /// <summary>
        /// Lädt alle Objekte vom Dateisystem.
        /// </summary>
        Task<IReadOnlyList<T>> LoadAllAsync();

        /// <summary>
        /// Speichert alle Objekte als JSON.
        /// </summary>
        Task SaveAllAsync(IEnumerable<T> items);

        /// <summary>
        /// Lädt ein einzelnes Objekt anhand des Namens oder der ID.
        /// </summary>
        Task<T?> LoadAsync(string name);

        /// <summary>
        /// Speichert ein einzelnes Objekt.
        /// </summary>
        Task SaveAsync(T item);

        /// <summary>
        /// Löscht ein einzelnes Objekt.
        /// </summary>
        Task DeleteAsync(string name);
    }
}
