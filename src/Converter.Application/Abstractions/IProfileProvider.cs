using System.Collections.Generic;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions
{
    public interface IProfileProvider
    {
        Task<IReadOnlyList<Converter.Models.ConversionProfile>> GetAllProfilesAsync();
        Task<Converter.Models.ConversionProfile> GetProfileByIdAsync(string id);
        Task<Converter.Models.ConversionProfile> GetDefaultProfileAsync();
        Task SetDefaultProfileAsync(string id);
        Task SaveProfileAsync(Converter.Models.ConversionProfile profile);
        Task DeleteProfileAsync(string id);
    }
}
