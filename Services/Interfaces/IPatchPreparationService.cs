using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VersionManager.Services.Interfaces
{
    public interface IPatchPreparationService
    {
        Task PreparePatchAsync(string rawPatchFolder, string preparedPatchFolder);
    }
}
